using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

// Authored locally under the user's seven-card exception. Individual approved
// portraits use the Wylder runtime folder; other cards retain native interim art.
public sealed class WylderVacuumSlice : WylderCard
{
    public WylderVacuumSlice() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new DynamicVar("Break", 20m), new PowerVar<WeakPower>("Weak", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<WeakPower>() });
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnBreak(PlayerChoiceContext context) =>
        await PowerCmd.Apply<WeakPower>(context, CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray(),
            DynamicVars["Weak"].BaseValue, Owner.Creature, this);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithWylderFx(WylderAttackVfx.Kind.VacuumSlice, Owner.Creature).Execute(context);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}

public sealed class WylderStormBlade : WylderCard
{
    public WylderStormBlade() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(4m, ValueProp.Move), new RepeatVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int hits = 1 + (Pursuing ? DynamicVars.Repeat.IntValue : 0);
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackSingle");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithHitCount(hits).WithWylderFx(WylderAttackVfx.Kind.StormBlade, Owner.Creature).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class WylderFlamingStrike : WylderCard
{
    public WylderFlamingStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move), new CardsVar(1), new RepeatVar(1)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(IsUpgraded));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        // One attack and one generated card per native CardPlay, including Replay.
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackSingle");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.FlameSlash, Owner.Creature).Execute(context);
        var coating = CombatState.CreateCard<WylderFireCoating>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(coating);
        await CardPileCmd.AddGeneratedCardToCombat(coating, PileType.Hand, Owner);
    }
}

// Generated colorless Skill, excluded from Wylder's reward pool.
public sealed class WylderFireCoating : CardModel
{
    public WylderFireCoating() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<ColorlessCardPool>();
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_fire_coating.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust, CardKeyword.Retain };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<VigorPower>("Vigor", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<VigorPower>() };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(2m);
}

public sealed class WylderFlameOfRedmanes : WylderCard
{
    public WylderFlameOfRedmanes() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(6m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 4m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackSingle");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithWylderFx(WylderAttackVfx.Kind.FireExplosion, Owner.Creature).Execute(context);
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["Vigor"].UpgradeValueBy(1m);
    }
}

public sealed class WylderBloodhoundsFinesse : WylderCard
{
    public WylderBloodhoundsFinesse() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(4m, ValueProp.Move), new BlockVar(4m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

public sealed class WylderLionsClaw : WylderCard
{
    public WylderLionsClaw() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(18m, ValueProp.Move), new DynamicVar("Break", 40m), new PowerVar<PhantomImbalancePower>("Imbalance", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<PhantomImbalancePower>() });
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnBreak(PlayerChoiceContext context)
    {
        if (BrokenTarget?.IsAlive != true) return;
        await PowerCmd.Apply<PhantomImbalancePower>(context, BrokenTarget,
            DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
        await PhantomImbalancePower.ResolveThreshold(context, BrokenTarget);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
    }
}

public sealed class WylderSpinningSlash : WylderCard
{
    public WylderSpinningSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/dagger_spray.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(4m, ValueProp.Move), new RepeatVar(2), new DynamicVar("Break", 12m), new CardsVar(1) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override int BreakHitCount => DynamicVars.Repeat.IntValue;
    protected override async Task OnBreak(PlayerChoiceContext context) => await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
