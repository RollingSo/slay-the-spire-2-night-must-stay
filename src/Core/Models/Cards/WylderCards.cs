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

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderCard : CardModel
{
    protected WylderCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }
    // Only the two Stamp cards have bespoke art commissioned in this batch.
    public override string PortraitPath => this switch
    {
        WylderStampUpwardCut or WylderStampSweep => $"res://images/packed/card_portraits/wylder/{Id.Entry.ToLowerInvariant()}.png",
        WylderDefend or WylderSlidingLeap => "res://images/packed/card_portraits/ironclad/defend_ironclad.png",
        WylderClawMovement => "res://images/packed/card_portraits/ironclad/barricade.png",
        _ => "res://images/packed/card_portraits/ironclad/strike_ironclad.png",
    };
    protected bool Pursuing => Owner?.Creature?.HasPower<WylderPursuitPower>() == true;
    protected virtual bool HasPursuitCondition => false;
    protected override bool ShouldGlowGoldInternal => HasPursuitCondition && Pursuing;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WylderPursuitPower>(),
        HoverTipFactory.FromPower<WylderBreakDescriptionPower>(),
    };
    // Future Break cards override these two members. Counters are per CardPlay,
    // per target, and only direct attack damage from this card is counted.
    protected virtual decimal BreakThreshold => 0m;
    protected virtual Task OnBreak(PlayerChoiceContext context) => Task.CompletedTask;
    private sealed class BreakData
    {
        public readonly Dictionary<Creature, decimal> Damage = new();
        public bool Triggered;
        public bool Resolving;
    }
    private static readonly ConditionalWeakTable<WylderCard, BreakData> BreakStates = new();
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card == this)
        {
            BreakData data = BreakStates.GetOrCreateValue(this);
            data.Damage.Clear(); data.Triggered = false; data.Resolving = true;
        }
        return Task.CompletedTask;
    }
    public override Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card == this) BreakStates.GetOrCreateValue(this).Resolving = false;
        return Task.CompletedTask;
    }
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature dealer,
        DamageResult result, ValueProp props, Creature target, CardModel cardSource)
    {
        BreakData data = BreakStates.GetOrCreateValue(this);
        if (cardSource != this || dealer != Owner.Creature || !props.IsPoweredAttack()
            || BreakThreshold <= 0 || !data.Resolving || data.Triggered || target.Side == dealer.Side) return;
        data.Damage.TryGetValue(target, out decimal total);
        total += result.UnblockedDamage;
        data.Damage[target] = total;
        if (total < BreakThreshold) return;
        data.Triggered = true; // Consume before awaiting to prevent reentrant triggers.
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
    public WylderClawMovement() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }
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
public sealed class WylderSlidingLeap : WylderCard
{
    public WylderSlidingLeap() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override bool HasPursuitCondition => true;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (pursuit) await PlayerCmd.GainEnergy(1, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
public sealed class WylderStampUpwardCut : WylderCard
{
    public WylderStampUpwardCut() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(16m, ValueProp.Move), new DynamicVar("StrengthLoss", 10m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (pursuit && play.Target.IsAlive)
            await PowerCmd.Apply<WylderUppercutStrengthDownPower>(context, play.Target,
                DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["StrengthLoss"].UpgradeValueBy(3m); }
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
