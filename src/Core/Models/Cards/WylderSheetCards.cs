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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

// Local completion of unfinished rows in the user's 2026-10-29 export.
// Dedicated artwork reviewed and integrated on 2026-10-29.
public sealed class WylderJumpingHeavy : WylderCard
{
    public WylderJumpingHeavy() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_jumping_heavy.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(9m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, p); await WylderPursuitPower.Enter(c, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
public sealed class WylderAerialStake : WylderCard
{
    public WylderAerialStake() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_aerial_stake.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(12m, ValueProp.Move), new DynamicVar("Break",18m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).Execute(c); if (BrokeThisPlay) await WylderPursuitPower.Enter(c, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
public sealed class WylderCounterRush : WylderCard
{
    public WylderCounterRush() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_counter_rush.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new BlockVar(5m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, p); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).Execute(c); }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars.Block.UpgradeValueBy(2m); }
}
public sealed class WylderDebut : WylderCard
{
    public WylderDebut() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_debut.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new EnergyVar(3) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await WylderPursuitPower.Enter(c, Owner.Creature, this); await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner); }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
public sealed class WylderSixthSense : WylderCard
{
    private bool _spent;
    public WylderSixthSense() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_sixth_sense.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Unplayable };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("HealPercent", 30m) };
    public override bool ShouldDie(Creature creature) => creature != Owner?.Creature || _spent
        || Pile?.Type is not (PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust or PileType.Play);
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (_spent || creature != Owner.Creature || creature.CurrentHp > 0) return;
        _spent = true;
        await CreatureCmd.Heal(creature, Math.Ceiling(creature.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m), false);
        var deck = DeckVersion;
        if (deck != null && deck.Pile?.Type == PileType.Deck) await CardPileCmd.RemoveFromDeck(deck, false);
        await CardPileCmd.RemoveFromCombat(this, false);
    }
    protected override void OnUpgrade() => DynamicVars["HealPercent"].UpgradeValueBy(20m);
}
public sealed class WylderClawRend : WylderFollowThroughCard
{
    public WylderClawRend() : base(1, CardRarity.Common) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_claw_rend.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move), new PowerVar<VulnerablePower>("Vulnerable", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>() });
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).Execute(c); if (p.Target.IsAlive) await PowerCmd.Apply<VulnerablePower>(c, p.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
public sealed class WylderMotionAndStillness : WylderCard
{
    public WylderMotionAndStillness() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_motion_and_stillness.png";
    public override bool GainsBlock => true;
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2), new BlockVar(6m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { bool pursuit = Pursuing; await CardPileCmd.Draw(c, DynamicVars.Cards.BaseValue, Owner); if (pursuit) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, p); }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
public sealed class WylderExecution : WylderCard
{
    public WylderExecution() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_execution.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => CreatureCmd.Stun(p.Target);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderDesperateSurvival : WylderCard
{
    public WylderDesperateSurvival() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_desperate_survival.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(14m,ValueProp.Move), new BlockVar(10m,ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { WylderDesperation.HoverTip });
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).Execute(c); await CreatureCmd.GainBlock(Owner.Creature,DynamicVars.Block.BaseValue,ValueProp.Move,p); }
    public override Task BeforeSideTurnEnd(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants) => side == CombatSide.Player && participants.Contains(Owner.Creature) ? WylderDesperation.AutoPlayTargeted(c,this,CombatState.GetOpponentsOf(Owner.Creature).ToArray()) : Task.CompletedTask;
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); DynamicVars.Block.UpgradeValueBy(3m); }
}
public sealed class WylderWildfire : WylderCard
{
    public WylderWildfire() : base(1,CardType.Attack,CardRarity.Rare,TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_wildfire.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m,ValueProp.Move), new DynamicVar("Break",20m), new PowerVar<VigorPower>("Vigor",4m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext c,CardPlay p)
    { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(c); if(BrokeThisPlay) await PowerCmd.Apply<VigorPower>(c,Owner.Creature,DynamicVars["Vigor"].BaseValue,Owner.Creature,this); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
public sealed class WylderBoldness : WylderAbilityCard<WylderBoldnessPower>
{
    public WylderBoldness() : base(1,CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_boldness.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Block",5m) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}
public sealed class WylderNightForm : WylderAbilityCard<WylderNightFormPower>
{
    public WylderNightForm() : base(3,CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_night_form.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1),new DynamicVar("Block",6m) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(3m);
}
public sealed class WylderRemainingEmber : WylderAbilityCard<WylderRemainingEmberPower>
{
    public WylderRemainingEmber() : base(2,CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_remaining_ember.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Damage",4m) };
    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(2m);
}
public sealed class WylderUndying : WylderAbilityCard<WylderUndyingPower>
{
    public WylderUndying() : base(1,CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_undying.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Heal",2m) };
    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(1m);
}
