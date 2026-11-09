using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

internal static class WylderPlayHistory
{
    // The native history inserts the current started entry before OnPlay. Stop
    // at its exact CardPlay identity so nested/later plays cannot change what
    // "previously this turn" means. Auto-plays and replays remain real entries.
    internal static bool HasPrevious(WylderCard card, CardPlay play, CardType? type = null) =>
        card.CombatState != null && CombatManager.Instance.History.CardPlaysStarted
            .TakeWhile(entry => entry.CardPlay != play)
            .Any(entry => entry.CardPlay.Card.Owner == card.Owner
                && entry.HappenedThisTurn(card.CombatState)
                && (type == null || entry.CardPlay.Card.Type == type));
}

// Reviewed B1 proposals from the user-designated planner. Native interim
// portraits only: the planner's artwork descriptions are not art approvals.
public sealed class WylderFlameReady : WylderCard
{
    public WylderFlameReady() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(5m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Vigor"].UpgradeValueBy(1m);
    }
}

public sealed class WylderSpineShock : WylderCard
{
    public WylderSpineShock() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new DynamicVar("Break", 13m), new PowerVar<WeakPower>("Weak", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<WeakPower>() });
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnBreak(PlayerChoiceContext context)
    {
        if (BrokenTarget?.IsAlive == true)
            await PowerCmd.Apply<WeakPower>(context, BrokenTarget, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderFlameMomentum : WylderCard
{
    public WylderFlameMomentum() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/defect/charge_battery.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move), new DynamicVar("VigorThreshold", 3m),
        new PowerVar<VigorPower>("Vigor", 2m), new EnergyVar(1)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (Owner.Creature.GetPowerAmount<VigorPower>() >= DynamicVars["VigorThreshold"].BaseValue)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        else
            await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderCliffbreaker : WylderCard
{
    public WylderCliffbreaker() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/tear_asunder.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(17m, ValueProp.Move), new DynamicVar("Break", 25m), new PowerVar<VigorPower>("Vigor", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.Greatsword, Owner.Creature).Execute(context);
        // This new card explicitly awards fresh Vigor after the native attack
        // (including old Vigor consumption), not to its own damage events.
        if (BrokeThisPlay)
            await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}

public sealed class WylderDualGreatswordLeap : WylderCard
{
    private bool _returnAfterPlay;
    public WylderDualGreatswordLeap() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new RepeatVar(2), new DynamicVar("Break", 20m), new EnergyVar(1) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override int BreakHitCount => DynamicVars.Repeat.IntValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
        if (!BrokeThisPlay) return;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        _returnAfterPlay = true;
    }
    public override async Task AfterCardChangedPilesLate(MegaCrit.Sts2.Core.Models.CardModel card, PileType oldPileType, MegaCrit.Sts2.Core.Models.AbstractModel clonedBy)
    {
        if (card != this || !_returnAfterPlay || Pile == null || Pile.Type == PileType.Play || !Pile.Type.IsCombatPile()) return;
        _returnAfterPlay = false;
        if (Pile.Type != PileType.Hand) await CardPileCmd.Add(this, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class WylderGhostflameExplosion : WylderCard
{
    public WylderGhostflameExplosion() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_ghostflame_explosion.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(15m, ValueProp.Move), new DynamicVar("Break", 30m), new PowerVar<FreezePower>("Freeze", 6m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<FreezePower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.GhostflameExplosion, Owner.Creature).Execute(context);
        if (BrokeThisPlay && BrokenTarget?.IsAlive == true)
            await PowerCmd.Apply<FreezePower>(context, BrokenTarget, DynamicVars["Freeze"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Freeze"].UpgradeValueBy(1m);
    }
}

public sealed class WylderTurningGuard : WylderCard
{
    public WylderTurningGuard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(7m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (attacked) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderShieldPress : WylderCard
{
    public WylderShieldPress() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/deflect.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(4m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool skilled = WylderPlayHistory.HasPrevious(this, play, CardType.Skill);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (skilled) await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class WylderSlidingThrust : WylderCard
{
    public WylderSlidingThrust() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/bash.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(7m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool first = !WylderPlayHistory.HasPrevious(this, play);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (first) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderSteadyBreath : WylderCard
{
    public WylderSteadyBreath() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(7m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (!attacked) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderGrappleSwing : WylderCard
{
    public WylderGrappleSwing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(6m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 2m), new EnergyVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (attacked) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        else await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderLightStepGuard : WylderCard
{
    public WylderLightStepGuard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/deflect.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(3m), new CalculationExtraVar(3m), new DynamicVar("HandThreshold", 2m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            ((WylderLightStepGuard)card).LowHand(card.DynamicVars["HandThreshold"].IntValue) ? 1m : 0m)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new IHoverTip[]
    {
        new HoverTip(new LocString("cards", "WYLDER_HAND_CONDITION.title"),
            new LocString("cards", "WYLDER_HAND_CONDITION.description"))
    });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(2m);
}

public sealed class WylderLowStance : WylderCard
{
    public WylderLowStance() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(8m, ValueProp.Move), new DynamicVar("HandThreshold", 3m), new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool low = LowHand(DynamicVars["HandThreshold"].IntValue);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (low) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderAerialRoll : WylderCard
{
    public WylderAerialRoll() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new DynamicVar("HandThreshold", 3m), new DynamicVar("CostReduction", 1m) };
    public override bool TryModifyEnergyCostInCombat(MegaCrit.Sts2.Core.Models.CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !WylderHandPlayContext.IsLowHand(this, null, DynamicVars["HandThreshold"].IntValue)) return false;
        modifiedCost = Math.Max(0m, originalCost - DynamicVars["CostReduction"].BaseValue);
        return true;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderPursuitSlash : WylderCard
{
    public WylderPursuitSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/bash.png";
    protected override bool HasPursuitCondition => true;
    protected override bool ShouldGlowGoldInternal => Pursuing && LowHand(DynamicVars["HandThreshold"].IntValue);
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(8m, ValueProp.Move), new DynamicVar("HandThreshold", 3m), new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool reward = Pursuing && LowHand(DynamicVars["HandThreshold"].IntValue);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (reward) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderGroundDash : WylderCard
{
    public WylderGroundDash() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(3m, ValueProp.Move), new DynamicVar("HandThreshold", 3m), new PowerVar<VigorPower>("Vigor", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool low = LowHand(DynamicVars["HandThreshold"].IntValue);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (low) await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
