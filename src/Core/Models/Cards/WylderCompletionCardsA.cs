using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

internal static class WylderCompletionRules
{
    internal static bool IsAttacking(Creature target) => target?.IsAlive == true
        && target.Monster?.NextMove?.Intents.OfType<AttackIntent>().Any() == true;

    internal static Creature[] AttackingEnemies(CardModel card) => card.CombatState?.GetOpponentsOf(card.Owner.Creature)
        .Where(IsAttacking).ToArray() ?? Array.Empty<Creature>();

    internal static bool PreviousWasAttack(WylderCard card, CardPlay play) => card.CombatState != null
        && CombatManager.Instance.History.CardPlaysStarted.TakeWhile(entry => entry.CardPlay != play)
            .LastOrDefault(entry => entry.CardPlay.Card.Owner == card.Owner && entry.HappenedThisTurn(card.CombatState))
            ?.CardPlay.Card.Type == CardType.Attack;

    // Equal current costs use hand order. X-cost and unplayable cards cannot
    // receive a meaningful relative energy discount and are not candidates.
    internal static CardModel HighestCostOther(CardModel source, CardType type) => PileType.Hand.GetPile(source.Owner).Cards
        .Where(card => card != source && card.Type == type && !card.EnergyCost.CostsX && card.EnergyCost.Canonical >= 0)
        .OrderByDescending(card => card.EnergyCost.GetWithModifiers(CostModifiers.All)).FirstOrDefault();

    internal static void DiscountHighestOther(CardModel source, CardType type, int amount)
    {
        CardModel target = HighestCostOther(source, type);
        if (target == null) return;
        target.EnergyCost.AddThisTurn(-amount);
        target.InvokeEnergyCostChanged();
    }
}

// Native portraits are temporary mechanics-test assets, not approved Wylder art.
public sealed class WylderMoltenSweep : WylderCard
{
    public WylderMoltenSweep() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(7m, ValueProp.Move), new BlockVar(4m, ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        if (WylderCombatEvents.ConsumedVigor(play))
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderClawPull : WylderCard
{
    public WylderClawPull() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_claw_pull.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new PowerVar<VulnerablePower>("Vulnerable", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target?.IsAlive == true)
            await PowerCmd.Apply<VulnerablePower>(context, play.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Vulnerable"].UpgradeValueBy(1m);
}

public sealed class WylderParry : WylderCard
{
    public WylderParry() : base(4, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool IsPlayable => CombatState == null || WylderCompletionRules.AttackingEnemies(this).Length > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
        { WylderDesperation.HoverTip, HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!WylderCompletionRules.IsAttacking(play.Target)) return;
        await CreatureCmd.Stun(play.Target);
        if (play.Target.IsStunned) await WylderCombatEvents.RegisterSkillStun(context, play);
    }
    public override Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        // OnTurnEndInHandWrapper moves to Play and always discards afterwards;
        // use the earlier side-end hook so the real hand and Retain stay intact.
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature)) return Task.CompletedTask;
        return WylderDesperation.AutoPlayTargeted(context, this, WylderCompletionRules.AttackingEnemies(this));
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class WylderSteppingSkewer : WylderCard
{
    public WylderSteppingSkewer() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(5m, ValueProp.Move), new RepeatVar(2), new EnergyVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
        if (WylderCombatEvents.ConsumedVigor(play)) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class WylderFlametongueSweep : WylderCard
{
    public WylderFlametongueSweep() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(7m, ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        if (WylderCombatEvents.ConsumedVigor(play)) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderRelentlessAdvance : WylderCard
{
    public WylderRelentlessAdvance() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/deflect.png";
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(3m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool previousAttack = WylderCompletionRules.PreviousWasAttack(this, play);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (previousAttack) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class WylderRampartBreaker : WylderCard
{
    public WylderRampartBreaker() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/tear_asunder.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(9m), new ExtraDamageVar(4m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (_, target) => target?.Block > 0 ? 1m : 0m),
        new DynamicVar("Break", 18m), new PowerVar<VulnerablePower>("Vulnerable", 2m)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>() });
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnBreak(PlayerChoiceContext context)
    {
        if (BrokenTarget?.IsAlive == true)
            await PowerCmd.Apply<VulnerablePower>(context, BrokenTarget, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.CalculatedDamage.Calculate(play.Target)).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderSwiftRetreat : WylderCard
{
    public WylderSwiftRetreat() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new DynamicVar("CostReduction", 1m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        WylderCompletionRules.DiscountHighestOther(this, CardType.Skill, DynamicVars["CostReduction"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderHiltGuard : WylderCard
{
    public WylderHiltGuard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/deflect.png";
    public override bool GainsBlock => true;
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(6m, ValueProp.Move), new PowerVar<WeakPower>("Weak", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<WeakPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (pursuit)
            await PowerCmd.Apply<WeakPower>(context, WylderCompletionRules.AttackingEnemies(this), DynamicVars["Weak"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderSidestep : WylderCard
{
    public WylderSidestep() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/backflip.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(4m, ValueProp.Move), new PowerVar<WeakPower>("Weak", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<WeakPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool previousAttack = WylderCompletionRules.PreviousWasAttack(this, play);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        Creature[] targets = WylderCompletionRules.AttackingEnemies(this);
        if (previousAttack && targets.Length > 0)
            await PowerCmd.Apply<WeakPower>(context, Owner.RunState.Rng.CombatTargets.NextItem(targets), DynamicVars["Weak"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class WylderTwinFeint : WylderCard
{
    public WylderTwinFeint() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(4m, ValueProp.Move), new RepeatVar(2), new CardsVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
        if (WylderCombatEvents.ConsumedVigor(play)) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class WylderOpeningPressure : WylderCard
{
    public WylderOpeningPressure() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/bash.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(9m), new ExtraDamageVar(4m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            WylderPlayHistory.HasPrevious((WylderCard)card, ((WylderOpeningPressure)card).ResolvingPlay, CardType.Attack) ? 0m : 1m),
        new DynamicVar("Break", 19m), new DynamicVar("CostReduction", 1m)
    };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage.Calculate(play.Target)).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (BrokeThisPlay) WylderCompletionRules.DiscountHighestOther(this, CardType.Attack, DynamicVars["CostReduction"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderCorpseWaxCutter : WylderCard
{
    public WylderCorpseWaxCutter() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_corpse_wax_cutter.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new DynamicVar("Break", 20m), new PowerVar<VulnerablePower>("Vulnerable", 2m), new PowerVar<WeakPower>("Weak", 2m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        if (BrokeThisPlay)
        {
            var enemies = CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray();
            await PowerCmd.Apply<VulnerablePower>(context, enemies, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(context, enemies, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class WylderMeasuredRetreat : WylderCard
{
    public WylderMeasuredRetreat() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(7m), new CalculationExtraVar(1m), new DynamicVar("HandThreshold", 3m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            ((WylderMeasuredRetreat)card).LowHand(card.DynamicVars["HandThreshold"].IntValue) ? card.Owner.Creature.GetPowerAmount<VigorPower>() : 0m)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new IHoverTip[]
    {
        HoverTipFactory.FromPower<VigorPower>(),
        new HoverTip(new LocString("cards", "WYLDER_HAND_CONDITION.title"), new LocString("cards", "WYLDER_HAND_CONDITION.description"))
    });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderCrisisPoise : WylderCard
{
    public WylderCrisisPoise() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(6m), new CalculationExtraVar(2m), new DynamicVar("MaxExtraBlock", 6m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) => Math.Min(3, WylderCompletionRules.AttackingEnemies(card).Length))
    };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}

public sealed class WylderFlameStride : WylderCard
{
    public WylderFlameStride() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new PowerVar<VigorPower>("Vigor", 3m), new DynamicVar("CostReduction", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
        WylderCompletionRules.DiscountHighestOther(this, CardType.Attack, DynamicVars["CostReduction"].IntValue);
    }
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(1m);
}
