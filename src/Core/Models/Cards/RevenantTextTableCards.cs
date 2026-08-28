using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Cards;

internal static class RevenantTextTableHelpers
{
    public static async Task DiscardFromDraw(
        CardModel source,
        PlayerChoiceContext context,
        LocString selectionPrompt,
        int count)
    {
        CardPile draw = PileType.Draw.GetPile(source.Owner);
        if (draw.Cards.Count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            context,
            draw,
            source.Owner,
            new CardSelectorPrefs(
                selectionPrompt,
                Math.Min(count, draw.Cards.Count)));
        foreach (CardModel card in selected)
            await CardPileCmd.Add(card, PileType.Discard);
    }

    public static async Task DamageAsFamily(
        CardModel source,
        PlayerChoiceContext context,
        decimal amount,
        bool all,
        int hits = 1)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(source.Owner);
        Creature family = manager.CurrentFamilyCreature;
        if (family == null) return;
        VigorPower vigor = family.GetPower<VigorPower>();
        decimal vigorToConsume = vigor?.Amount ?? 0m;
        bool attacked = false;
        for (int i = 0; i < hits; i++)
        {
            Creature[] enemies = source.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            if (enemies.Length == 0) break;
            attacked = true;
            if (all)
                await CreatureCmd.Damage(context, enemies, amount, ValueProp.Move, family, source);
            else
                await CreatureCmd.Damage(context, source.Owner.RunState.Rng.CombatTargets.NextItem(enemies), amount, ValueProp.Move, family, source);
        }
        if (attacked && vigor is not null && vigorToConsume > 0m)
            await PowerCmd.ModifyAmount(context, vigor, -vigorToConsume, family, source);
    }
}

public sealed class FrenziedThreeFingers : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(2m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/frenzied_three_fingers.png";
    public FrenziedThreeFingers() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<FrenziedThreeFingersPower>(context, Owner.Creature, DynamicVars.Damage.BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class FormationBreakerHammer : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();
    public override string PortraitPath => "res://revenant_assets/cards/formation_breaker_hammer.png";
    public FormationBreakerHammer() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner);
        await manager.TriggerResonance(context);
        if (manager.CurrentFamilyId == RevenantFamilyId.PumpkinHead)
            await RevenantTextTableHelpers.DamageAsFamily(this, context, 27m, false);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class LifeAndDeath : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/life_and_death.png";
    public LifeAndDeath() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Creature family = RevenantSummonManager.For(Owner).CurrentFamilyCreature;
        int block = Owner.Creature.Block;
        if (family == null || block <= 0) return;
        await CreatureCmd.LoseBlock(Owner.Creature, block);
        await CreatureCmd.GainMaxHp(family, block);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class GiantSkeletonWrath : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(13m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/giant_skeleton_wrath.png";
    public GiantSkeletonWrath() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).Execute(context);
        RevenantSummonManager manager = RevenantSummonManager.For(Owner);
        if (manager.CurrentFamilyId == RevenantFamilyId.Skeleton)
            await RevenantTextTableHelpers.DamageAsFamily(this, context, 5m, true, 4);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class SkyRendingChord : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new CardsVar(1) };
    public override string PortraitPath => "res://revenant_assets/cards/sky_rending_chord.png";
    public SkyRendingChord() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).Execute(context);
        await RevenantTextTableHelpers.DiscardFromDraw(this, context, SelectionScreenPrompt, DynamicVars.Cards.IntValue);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class SubstituteDoll : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new CardsVar(1) };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/substitute_doll.png";
    public SubstituteDoll() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await RevenantTextTableHelpers.DiscardFromDraw(this, context, SelectionScreenPrompt, 1);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class SpiritGathering : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1), new CardsVar(3) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/spirit_gathering.png";
    public SpiritGathering() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await RevenantCardHelpers.AddFromDiscard(this, context, DynamicVars.Cards.IntValue, false);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class Concerto : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/concerto.png";
    public Concerto() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
        await RevenantSummonManager.For(Owner).TriggerResonance(context);
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class FightForMe : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("DexterityLoss", 3m) };
    public override string PortraitPath => "res://revenant_assets/cards/fight_for_me.png";
    public FightForMe() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, -DynamicVars["DexterityLoss"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<FightForMePower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["DexterityLoss"].UpgradeValueBy(-1m);
}

public sealed class SoulCursingBell : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(4m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/soul_cursing_bell.png";
    public SoulCursingBell() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).Execute(context);
    }
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this || !RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType) || Pile?.Type != PileType.Discard) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class LightSpirit : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/light_spirit.png";
    public LightSpirit() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<LightSpiritPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Grooming : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/grooming.png";
    public Grooming() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await RevenantTextTableHelpers.DiscardFromDraw(this, context, SelectionScreenPrompt, 1);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class ReanimateDead : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/reanimate_dead.png";
    public ReanimateDead() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
        await RevenantSummonManager.For(Owner).ReviveDeadEnemy(context);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class SoulReturn : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(4m, ValueProp.Move), new CardsVar(1) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/soul_return.png";
    public SoulReturn() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await RevenantCardHelpers.AddFromDiscard(this, context, DynamicVars.Cards.IntValue, false);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class HeavyEcho : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/heavy_echo.png";
    public HeavyEcho() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<HeavyEchoPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class ChantingBlessing : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(8m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/chanting_blessing.png";
    public ChantingBlessing() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<ChantingBlessingPower>(context, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class UnderworldReflection : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/underworld_reflection.png";
    public UnderworldReflection() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => RevenantSummonManager.For(Owner).ReviveRandomNecro(context);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class SpiritManipulation : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(10m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/spirit_manipulation.png";
    public SpiritManipulation() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        Creature target = cardPlay.Target;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(context);
        if (!target.IsAlive)
            RevenantSummonManager.For(Owner).MarkForNextCombat(target);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

public sealed class PreparationRitual : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();
    public override string PortraitPath => "res://revenant_assets/cards/preparation_ritual.png";
    public PreparationRitual() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        IReadOnlyList<CardModel> recovered = await RevenantCardHelpers.AddFromDiscard(this, context, 1, false);
        if (recovered.FirstOrDefault() is IRevenantChargeCard chargeCard)
            await chargeCard.CompleteCharge(context);
    }
    protected override void OnUpgrade() { }
}

public sealed class WatchfulWaiting : CardModel, IRevenantChargeCard
{
    private bool _chargeComplete;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new BoolVar("Ready") };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/watchful_waiting.png";
    public bool IsChargeComplete => _chargeComplete;
    public override TargetType TargetType =>
        _chargeComplete ? TargetType.Self : TargetType.AnyEnemy;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };
    public WatchfulWaiting() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete);
    private WatchfulWaiting CreateOppositeChargePreview()
    {
        var preview = (WatchfulWaiting)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }
    private void SetChargePreviewState(bool complete)
    {
        _chargeComplete = complete;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!_chargeComplete && cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (!_chargeComplete)
            return;

        _chargeComplete = false;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
        await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
    }
    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (_chargeComplete) return;
        _chargeComplete = true;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted(this);
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class AllSoulsReturn : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? new[] { CardKeyword.Exhaust, CardKeyword.Retain } : new[] { CardKeyword.Exhaust };
    public override string PortraitPath => "res://revenant_assets/cards/all_souls_return.png";
    public AllSoulsReturn() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardPile discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            context, discard, Owner,
            new CardSelectorPrefs(new LocString("cards", "REVENANT_RECOVER_ANY_CARDS"), 0, discard.Cards.Count));
        await CardPileCmd.Add(selected, PileType.Hand);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class FollowingShadow : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/following_shadow.png";
    public FollowingShadow() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<FollowingShadowPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
