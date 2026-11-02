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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

// P050–P075 non-Power completion. Native portraits are test assets, not an
// assertion that custom artwork has been approved. Pending effects are owned
// by the shared CardPlay event layer, not by these card instances.
public sealed class WylderAirCleavingChase : WylderCard
{
    public WylderAirCleavingChase() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/tear_asunder.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(20m, ValueProp.Move), new DynamicVar("Break", 38m), new BlockVar(8m, ValueProp.Move) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (BrokeThisPlay)
        {
            if (BrokenTarget?.IsAlive == true) await CreatureCmd.Stun(BrokenTarget);
        }
        else await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}

public sealed class WylderTakersFlames : WylderCard
{
    public WylderTakersFlames() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_takers_flames.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(16m, ValueProp.Move), new DynamicVar("Break", 30m), new DynamicVar("Heal", 6m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        // A successful Break remains valid even if its target died. Heal once,
        // after every native AOE target, never once per damaged enemy.
        if (BrokeThisPlay) await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}

public sealed class WylderInfernalSlash : WylderCard
{
    public WylderInfernalSlash() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(17m), new ExtraDamageVar(10m), new DynamicVar("VigorThreshold", 4m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            ((WylderInfernalSlash)card).Qualifies ? 1m : 0m)
    };
    private bool Qualifies => WylderHandPlayContext.SourceVigor(this, ResolvingPlay) is int vigor
        && vigor >= DynamicVars["VigorThreshold"].IntValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderAssaultDrop : WylderCard
{
    public WylderAssaultDrop() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(10m, ValueProp.Move), new RepeatVar(2), new DynamicVar("Break", 32m), new PowerVar<PhantomImbalancePower>("Imbalance", 1m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<PhantomImbalancePower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
        if (!BrokeThisPlay) return;
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(c => c.IsAlive).ToArray())
        {
            await PowerCmd.Apply<PhantomImbalancePower>(context, enemy, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
            await PhantomImbalancePower.ResolveThreshold(context, enemy);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class WylderCounteroffensive : WylderCard
{
    public WylderCounteroffensive() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    public override bool GainsBlock => true;
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(15m), new ExtraDamageVar(3m), new DynamicVar("BlockStep", 5m), new DynamicVar("MaxBonus", 12m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => card.IsMutable && card.Owner != null
            ? Math.Min(4m, Math.Floor(card.Owner.Creature.Block / card.DynamicVars["BlockStep"].BaseValue)) : 0m),
        new BlockVar(7m, ValueProp.Move)
    };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (pursuit) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    }
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(4m);
}

public sealed class WylderFlameWard : WylderCard
{
    public WylderFlameWard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/impervious.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? new[] { CardKeyword.Exhaust, CardKeyword.Retain } : new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(0m), new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            card.IsMutable && card.Owner != null ? card.Owner.Creature.GetPowerAmount<VigorPower>() : 0m)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, play);
        WylderCombatEvents.PreserveHalfVigor(Owner);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class WylderGrappleLeap : WylderCard
{
    public WylderGrappleLeap() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("CostReduction", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new IHoverTip[]
    {
        new HoverTip(new LocString("cards", "WYLDER_GRAPPLE_LEAP_PENDING.title"), new LocString("cards", "WYLDER_GRAPPLE_LEAP_PENDING.description"))
    });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
        WylderCombatEvents.SetNextHandAttackDiscount(Owner, DynamicVars["CostReduction"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars["CostReduction"].UpgradeValueBy(1m);
}

public sealed class WylderForcedBreakout : WylderCard
{
    public WylderForcedBreakout() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/tear_asunder.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DynamicVar("CostReduction", 1m), new DynamicVar("HandThreshold", 3m) };
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = PileType.Hand.GetPile(Owner).Cards.Where(card => card != this && !card.EnergyCost.CostsX && card.EnergyCost.Canonical >= 0)
            .OrderByDescending(card => card.EnergyCost.GetResolved()).FirstOrDefault();
        if (target != null)
        {
            target.EnergyCost.AddThisTurnOrUntilPlayed(-DynamicVars["CostReduction"].IntValue - (LowHand(DynamicVars["HandThreshold"].IntValue) ? 1 : 0));
            target.InvokeEnergyCostChanged();
        }
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars["CostReduction"].UpgradeValueBy(1m);
}

public sealed class WylderSpendReserves : WylderCard
{
    public WylderSpendReserves() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new DynamicVar("VigorStep", 3m), new DynamicVar("MaxTriggers", 2m), new EnergyVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        var vigor = Owner.Creature.GetPower<VigorPower>();
        int count = Math.Min(DynamicVars["MaxTriggers"].IntValue, Math.Max(0, vigor?.Amount ?? 0) / DynamicVars["VigorStep"].IntValue);
        if (count <= 0) return;
        await PowerCmd.ModifyAmount(context, vigor, -count * DynamicVars["VigorStep"].BaseValue, Owner.Creature, this);
        await PlayerCmd.GainEnergy(count * DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public sealed class WylderCliffStep : WylderCard
{
    public WylderCliffStep() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(10m, ValueProp.Move), new DynamicVar("CostReduction", 2m), new CardsVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        WylderCombatEvents.SetNextHandBreakDiscount(Owner, DynamicVars["CostReduction"].IntValue, DynamicVars.Cards.IntValue);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(4m); DynamicVars.Cards.UpgradeValueBy(1m); }
}

public sealed class WylderNightRespite : WylderCard
{
    public WylderNightRespite() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain, CardKeyword.Exhaust };
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(1), new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool refund = SourceHandCount != null && WylderCombatEvents.ConsumedOldPursuit(play);
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        if (refund) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class WylderWindChase : WylderCard
{
    public WylderWindChase() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new DynamicVar("CostReduction", 1m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        WylderCombatEvents.SetNextHandSkillDiscount(Owner, DynamicVars["CostReduction"].IntValue);
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public sealed class WylderSprintAfterimage : WylderCard
{
    public WylderSprintAfterimage() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(6m), new CalculationExtraVar(3m), new DynamicVar("MaxBonus", 12m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) => ((WylderSprintAfterimage)card).PreviousAttacks)
    };
    private int PreviousAttacks => !IsMutable || Owner == null || CombatState == null ? 0 : Math.Min(4,
        CombatManager.Instance.History.CardPlaysStarted.TakeWhile(entry => entry.CardPlay != ResolvingPlay)
            .Count(entry => entry.CardPlay.Card.Owner == Owner && entry.CardPlay.Card.Type == CardType.Attack && entry.HappenedThisTurn(CombatState)));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderLastEmber : WylderCard
{
    public WylderLastEmber() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(10m, ValueProp.Move), new DynamicVar("VigorCost", 6m), new DynamicVar("Heal", 5m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        var vigor = Owner.Creature.GetPower<VigorPower>();
        if (vigor == null || vigor.Amount < DynamicVars["VigorCost"].BaseValue) return;
        await PowerCmd.ModifyAmount(context, vigor, -DynamicVars["VigorCost"].BaseValue, Owner.Creature, this);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}

public sealed class WylderAllInDuel : WylderCard
{
    public WylderAllInDuel() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/impervious.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(12m, ValueProp.Move), new DynamicVar("HandThreshold", 3m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int reduction = LowHand(DynamicVars["HandThreshold"].IntValue) ? 2 : 1;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        foreach (var card in PileType.Hand.GetPile(Owner).Cards.Where(card => card != this && card.Type == CardType.Attack && !card.EnergyCost.CostsX && card.EnergyCost.Canonical >= 0))
        {
            card.EnergyCost.AddThisTurnOrUntilPlayed(-reduction);
            card.InvokeEnergyCostChanged();
        }
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}

public sealed class WylderLongNightOath : WylderCard
{
    public WylderLongNightOath() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ancient_beta.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(12m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 4m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
        WylderCombatEvents.AddNextTurnVigor(Owner, DynamicVars["Vigor"].IntValue);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(4m); DynamicVars["Vigor"].UpgradeValueBy(2m); }
}
