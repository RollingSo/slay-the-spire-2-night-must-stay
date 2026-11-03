using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
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
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderCard : CardModel
{
    protected WylderCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }
    public override string PortraitPath => $"res://images/packed/card_portraits/wylder/{Id.Entry.ToLowerInvariant()}.png";
    protected bool Pursuing => Owner?.Creature?.HasPower<WylderPursuitPower>() == true;
    protected virtual bool HasPursuitCondition => false;
    protected override bool ShouldGlowGoldInternal => (HasPursuitCondition && Pursuing) || CanBreakFromHand();

    private bool CanBreakFromHand()
    {
        if (!IsMutable || Pile?.Type != PileType.Hand || !HasBreak || CombatState == null) return false;
        decimal threshold = WylderCombatEvents.EffectiveBreak(this, null, BreakThreshold);
        int hits = DynamicVars.TryGetValue("Repeat", out var repeat) ? repeat.IntValue : 1;
        if (EnergyCost.CostsX) hits += MegaCrit.Sts2.Core.Hooks.Hook.ModifyXValue(CombatState, this, Owner.PlayerCombatState.Energy);
        if (hits <= 0) return false;
        foreach (Creature enemy in CombatState.GetOpponentsOf(Owner.Creature))
        {
            if (!enemy.IsHittable || !enemy.IsAlive) continue;
            decimal amount = DynamicVars.TryGetValue("CalculatedDamage", out _)
                ? DynamicVars.CalculatedDamage.Calculate(enemy) : DynamicVars.Damage.BaseValue;
            decimal perHit = Math.Floor(AttackVfxDamage.Preview(enemy, Owner.Creature, amount, ValueProp.Move, this));
            if (perHit * hits >= threshold) return true;
        }
        return false;
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WylderPursuitPower>(),
        HoverTipFactory.FromPower<WylderBreakDescriptionPower>(),
    };
    // Break counters are per CardPlay,
    // per target, and only direct attack damage from this card is counted.
    protected virtual decimal BreakThreshold => 0m;
    public bool HasBreak => BreakThreshold > 0m;
    protected virtual Task OnBreak(PlayerChoiceContext context) => Task.CompletedTask;
    protected CardPlay ResolvingPlay => BreakStates.GetOrCreateValue(this).Play;
    protected Creature BrokenTarget => BreakStates.GetOrCreateValue(this).Target;
    protected bool BrokeThisPlay => BreakStates.GetOrCreateValue(this).Triggered;
    protected bool LowHand(int threshold) => WylderHandPlayContext.IsLowHand(this, ResolvingPlay, threshold);
    protected int? SourceHandCount => WylderHandPlayContext.HandCount(this, ResolvingPlay);
    private sealed class BreakData
    {
        public readonly Dictionary<Creature, decimal> Damage = new();
        public bool Triggered;
        public bool Resolving;
        public CardPlay Play;
        public Creature Target;
    }
    private static readonly ConditionalWeakTable<WylderCard, BreakData> BreakStates = new();
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card == this)
        {
            WylderCombatEvents.BeginCard(play);
            BreakData data = BreakStates.GetOrCreateValue(this);
            data.Damage.Clear(); data.Triggered = false; data.Resolving = true; data.Play = play; data.Target = null;
        }
        return Task.CompletedTask;
    }
    public override Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card == this)
        {
            BreakData data = BreakStates.GetOrCreateValue(this);
            data.Resolving = false;
            data.Play = null;
            data.Target = null;
        }
        return Task.CompletedTask;
    }
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature dealer,
        DamageResult result, ValueProp props, Creature target, CardModel cardSource)
    {
        BreakData data = BreakStates.GetOrCreateValue(this);
        if (cardSource != this || dealer != Owner.Creature || !props.IsPoweredAttack()
            || BreakThreshold <= 0 || !data.Resolving || data.Triggered || target.Side == dealer.Side) return;
        data.Damage.TryGetValue(target, out decimal total);
        total += result.TotalDamage;
        data.Damage[target] = total;
        if (total < WylderCombatEvents.EffectiveBreak(this, data.Play, BreakThreshold)) return;
        data.Triggered = true; // Consume before awaiting to prevent reentrant triggers.
        data.Target = target;
        WylderCombatEvents.RegisterBreak(data.Play);
        await OnBreak(context);
    }
}

public sealed class WylderStrike : WylderCard
{
    public WylderStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(6m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
public sealed class WylderDefend : WylderCard
{
    public WylderDefend() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(5m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
public sealed class WylderClawMovement : WylderCard
{
    public WylderClawMovement() : base(1, CardType.Attack, CardRarity.Basic, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        WylderPursuitPower.Enter(context, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderDashAttack : WylderCard
{
    public WylderDashAttack() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new PowerVar<VulnerablePower>("Vulnerable", 1m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (pursuit && play.Target.IsAlive)
            await PowerCmd.Apply<VulnerablePower>(context, play.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Vulnerable"].UpgradeValueBy(1m); }
}
public sealed class WylderSlidingLeap : WylderFollowThroughCard
{
    public WylderSlidingLeap() : base(2, CardRarity.Common, CardType.Skill, TargetType.Self) { }
    public override int MaxUpgradeLevel => 0;
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        WylderPursuitPower.Enter(context, Owner.Creature, this);
}

public sealed class WylderCenteringBreath : WylderCard
{
    public WylderCenteringBreath() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override bool HasPursuitCondition => true;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(6m, ValueProp.Move), new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (pursuit) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
public sealed class WylderStampUpwardCut : WylderCard
{
    public WylderStampUpwardCut() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(16m, ValueProp.Move), new DynamicVar("StrengthLoss", 5m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (pursuit && play.Target.IsAlive)
            await PowerCmd.Apply<WylderUppercutStrengthDownPower>(context, play.Target,
                DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["StrengthLoss"].UpgradeValueBy(1m); }
}
public sealed class WylderStampSweep : WylderCard
{
    public WylderStampSweep() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(10m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this);
        if (Pursuing) await attack.TargetingAllOpponents(CombatState).Execute(context);
        else await attack.Targeting(play.Target).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
