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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Cards;

public sealed class DeadRealmSpiritFire : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<FreezePower>("Freeze", 4m),
    };

    public override string PortraitPath => "res://revenant_assets/cards/dead_realm_spirit_fire.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<FreezePower>() };

    public DeadRealmSpiritFire() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(context);
        await PowerCmd.Apply<FreezePower>(
            context,
            CombatState.HittableEnemies,
            DynamicVars["Freeze"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(1m);
}

public sealed class IceLightningSpear : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<FreezePower>("Freeze", 2m),
        new DynamicVar("BonusFreeze", 2m),
        new DynamicVar("CalculatedFreeze", 2m),
    };

    public override string PortraitPath => "res://revenant_assets/cards/ice_lightning_spear.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<FreezePower>() };

    public IceLightningSpear() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
        if (!cardPlay.Target.IsAlive)
            return;
        decimal freeze = DynamicVars["CalculatedFreeze"].BaseValue;
        await PowerCmd.Apply<FreezePower>(context, cardPlay.Target, freeze, Owner.Creature, this);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
        {
            DynamicVars["CalculatedFreeze"].BaseValue =
                DynamicVars["Freeze"].BaseValue + DynamicVars["BonusFreeze"].BaseValue;
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Creature.Side)
        {
            DynamicVars["CalculatedFreeze"].BaseValue = DynamicVars["Freeze"].BaseValue;
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Freeze"].UpgradeValueBy(1m);
        DynamicVars["CalculatedFreeze"].UpgradeValueBy(1m);
    }
}

public sealed class NecroDrive : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/necro_drive.png";

    public NecroDrive() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        RevenantSummonManager.For(Owner).ReviveRandomNecro(context);

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.AddUntilPlayed(-1, true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BoneCoin : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/bone_coin.png";

    public BoneCoin() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        int handCount = Owner.PlayerCombatState.Hand.Cards.Count;
        List<CardModel> selected = (await CardSelectCmd.FromHandForDiscard(
            context,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, handCount),
            null,
            this)).ToList();
        foreach (CardModel card in selected)
            await CardCmd.Discard(context, card);
        if (selected.Count > 0)
            await RevenantCardHelpers.AddFromDiscard(this, context, selected.Count, false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Harmony : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<VigorPower>(3m),
    };

    public override string PortraitPath => "res://revenant_assets/cards/harmony.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<VigorPower>() };

    public Harmony() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
        Creature family = RevenantSummonManager.For(Owner).CurrentFamilyCreature;
        if (family is { IsAlive: true })
            await PowerCmd.Apply<VigorPower>(
                context,
                family,
                DynamicVars["VigorPower"].BaseValue,
                Owner.Creature,
                this);
    }

    protected override void OnUpgrade() => DynamicVars["VigorPower"].UpgradeValueBy(2m);
}

public sealed class GhostlyTouch : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new PowerVar<GhostlyTouchPower>("Freeze", 1m) };

    public override string PortraitPath => "res://revenant_assets/cards/ghostly_touch.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<FreezePower>() };

    public GhostlyTouch() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<GhostlyTouchPower>(
            context,
            Owner.Creature,
            DynamicVars["Freeze"].BaseValue,
            Owner.Creature,
            this);

    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(1m);
}
