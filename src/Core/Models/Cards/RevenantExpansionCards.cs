using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Cards;

public sealed class GurranqsRock : CardModel
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(8m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/gurranqs_rock.png";

    public GurranqsRock() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        int xValue = ResolveEnergyXValue();
        await RevenantCardHelpers.DamageRandomEachHit(
            this,
            context,
            DynamicVars.Damage.BaseValue,
            xValue);
        if (xValue >= 2)
            await RevenantSummonManager.For(Owner).TriggerResonance(context);
        if (xValue >= 3)
            await RevenantCall.ChooseFamilyAndCall(context, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class FrenziedFlame : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DynamicVar("DamageMultiplier", 2m) };
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded
            ? new[] { CardKeyword.Exhaust, CardKeyword.Retain }
            : new[] { CardKeyword.Exhaust };
    protected override bool IsPlayable => RevenantSummonManager.For(Owner).HasLivingFamily;
    public override string PortraitPath => "res://revenant_assets/cards/frenzied_flame.png";

    public FrenziedFlame() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Creature family = Owner.Osty;
        if (family is not { IsAlive: true })
            return;
        decimal hpBefore = family.CurrentHp;
        await RevenantCardHelpers.DamageFamily(this, context, hpBefore);
        decimal hpLost = Math.Max(0m, hpBefore - family.CurrentHp);
        if (hpLost <= 0m)
            return;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(hpLost * DynamicVars["DamageMultiplier"].BaseValue)
            .CompatFromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class Ensemble : CardModel, IRevenantChargeCard
{
    private bool _chargeComplete;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new BlockVar(4m, ValueProp.Move), new BoolVar("Ready") };

    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/ensemble.png";

    [SavedProperty]
    public bool ChargeComplete
    {
        get => _chargeComplete;
        set
        {
            AssertMutable();
            _chargeComplete = value;
            ((BoolVar)DynamicVars["Ready"]).BoolVal = value;
        }
    }

    public bool IsChargeComplete => ChargeComplete;
    public override TargetType TargetType =>
        IsChargeComplete ? TargetType.Self : TargetType.AnyEnemy;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };

    public Ensemble() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete);

    private Ensemble CreateOppositeChargePreview()
    {
        var preview = (Ensemble)MutableClone();
        preview.ChargeComplete = !IsChargeComplete;
        return preview;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!IsChargeComplete && cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (!IsChargeComplete)
            return;

        ChargeComplete = false;
        await RevenantSummonManager.For(Owner).TriggerResonance(context);
        await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
    }

    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (IsChargeComplete)
            return;
        ChargeComplete = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted(this);
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class Surge : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/surge.png";

    public Surge() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        RevenantSummonManager.For(Owner).TriggerResonance(context);

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.AddUntilPlayed(-1, true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class UnderworldRising : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/underworld_rising.png";

    public UnderworldRising() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        RevenantCall.ChooseFamilyAndCall(context, Owner);

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.AddUntilPlayed(-1, true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Resurgence : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/resurgence.png";

    public Resurgence() : base(4, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.AddUntilPlayed(-1, true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// Public Beta added an official Soulbound card. Model IDs are derived from
// the simple type name rather than the namespace, so use a mod-prefixed name.
public sealed class NightMustStaySoulbound : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();
    public override string PortraitPath => "res://revenant_assets/cards/soulbound.png";

    public NightMustStaySoulbound() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        List<CardModel> selected = (await CardSelectCmd.FromHandForDiscard(
            context,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            null,
            this)).ToList();
        foreach (CardModel card in selected)
            await CardCmd.Discard(context, card);
        if (selected.Count > 0)
            await RevenantCardHelpers.AddFromDiscard(this, context, selected.Count, false);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class AnswerTheCall : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new CardsVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/answer_the_call.png";

    public AnswerTheCall() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        IEnumerable<CardModel> selected = await CardSelectCmd.FromHandForDiscard(
            context,
            Owner,
            new CardSelectorPrefs(
                CardSelectorPrefs.DiscardSelectionPrompt,
                Math.Min(DynamicVars.Cards.IntValue, Owner.PlayerCombatState.Hand.Cards.Count)),
            null,
            this);
        foreach (CardModel card in selected)
            await CardCmd.Discard(context, card);
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(-1m);
}

public sealed class RevenantCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new PowerVar<BufferPower>("Buffer", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] { HoverTipFactory.FromPower<BufferPower>() };
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    public override string PortraitPath => "res://revenant_assets/cards/revenant_card.png";

    public RevenantCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        foreach (var teammate in CombatState.Players.Where(player =>
                     player != Owner && player.Creature.IsAlive))
        {
            await PowerCmd.Apply<BufferPower>(
                context,
                teammate.Creature,
                DynamicVars["Buffer"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Buffer"].UpgradeValueBy(1m);
}

public sealed class KingsRecovery : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DynamicVar("Heal", 6m) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    public override string PortraitPath => "res://revenant_assets/cards/kings_recovery.png";

    public KingsRecovery() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive))
            await CreatureCmd.Heal(player.Creature, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(2m);
}

public sealed class UndyingMarch : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust, CardKeyword.Retain };
    protected override bool IsPlayable => RevenantSummonManager.For(Owner).HasLivingFamily;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[] { HoverTipFactory.FromPower<UndyingMarchPower>() };
    public override string PortraitPath => "res://revenant_assets/cards/undying_march.png";

    public UndyingMarch() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        Owner.Osty is { IsAlive: true } family
            ? PowerCmd.Apply<UndyingMarchPower>(context, family, 1m, Owner.Creature, this)
            : Task.CompletedTask;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
