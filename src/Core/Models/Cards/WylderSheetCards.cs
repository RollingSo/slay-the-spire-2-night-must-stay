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
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

// Local completion of unfinished rows in the user's 2026-10-29 export.
// Dedicated artwork reviewed and integrated on 2026-10-29.
public sealed class WylderJumpingHeavy : WylderCard
{
    public WylderJumpingHeavy() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    public override bool TryModifyEnergyCostInCombat(MegaCrit.Sts2.Core.Models.CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !Pursuing) return false;
        modifiedCost = 0;
        return true;
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(13m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.Greatsword, Owner.Creature).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
public sealed class WylderAerialStake : WylderCard
{
    public WylderAerialStake() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(20m, ValueProp.Move), new PowerVar<PhantomImbalancePower>("Imbalance", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<PhantomImbalancePower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        // Pursuit may be consumed by another listener during the same CardPlay.
        // Use the state captured before resolution so the card cannot become a no-op.
        if (!Pursuing && !WylderCombatEvents.WasPursuingAtPlayStart(play)) return;
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "Ultimate");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.FireExplosion, Owner.Creature).Execute(context);
        if (!play.Target.IsAlive) return;
        await PowerCmd.Apply<PhantomImbalancePower>(context, play.Target, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
        await PhantomImbalancePower.ResolveThreshold(context, play.Target);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderCounterRush : WylderDesperationCard
{
    public WylderCounterRush() : base(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(12m, ValueProp.Move), new PowerVar<PhantomImbalancePower>("Imbalance", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(HoverTipFactory.FromPower<PhantomImbalancePower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "Ultimate");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithWylderFx(WylderAttackVfx.Kind.FireExplosion, Owner.Creature).Execute(context);
        if (!play.Target.IsAlive) return;
        await PowerCmd.Apply<PhantomImbalancePower>(context, play.Target, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
        await PhantomImbalancePower.ResolveThreshold(context, play.Target);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Imbalance"].UpgradeValueBy(1m);
    }
}
public sealed class WylderDebut : WylderCard
{
    public WylderDebut() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_debut.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new EnergyVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    { await WylderPursuitPower.Enter(c, Owner.Creature, this); await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner); }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
public sealed class WylderSixthSense : WylderDesperationCard
{
    private bool _spent;
    public WylderSixthSense() : base(4, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_sixth_sense.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(10m, ValueProp.Move), new DynamicVar("HealPercent", 30m) };
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
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
    { WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "ClawShot"); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).WithWylderFx(WylderAttackVfx.Kind.Claw, Owner.Creature).Execute(c); if (p.Target.IsAlive) await PowerCmd.Apply<VulnerablePower>(c, p.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars["Vulnerable"].UpgradeValueBy(1m);
}
public sealed class WylderMotionAndStillness : WylderFollowThroughCard
{
    public WylderMotionAndStillness() : base(1, CardRarity.Uncommon, CardType.Skill, TargetType.Self) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(6m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 6m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var previous = CombatManager.Instance.History.CardPlaysStarted
            .TakeWhile(entry => entry.CardPlay != play)
            .LastOrDefault(entry => entry.CardPlay.Card.Owner == Owner);
        if (previous == null) return;
        if (previous.CardPlay.Card.Type == CardType.Attack)
            await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
        else
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Vigor"].UpgradeValueBy(2m);
    }
}
public sealed class WylderDesperateSurvival : WylderDesperationCard
{
    public WylderDesperateSurvival() : base(4, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Threshold", 50m) };
    internal bool CanExecute(Creature target) => target?.IsAlive == true && target.IsHittable
        && target.CurrentHp < DynamicVars["Threshold"].BaseValue;
    protected override IEnumerable<Creature> LegalDesperationTargets() =>
        CombatState.GetOpponentsOf(Owner.Creature).Where(CanExecute);
    protected override bool IsPlayable => CombatState == null || LegalDesperationTargets().Any();
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!CanExecute(play.Target)) return Task.CompletedTask;
        WylderAnimationPatch.PlayImmediate(Owner.Creature, "AttackTwoHand");
        WylderAttackEffects.Play(play.Target, WylderAttackVfx.Kind.Execution, Owner.Creature);
        return CreatureCmd.Kill(play.Target);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
public sealed class WylderWildfire : WylderAbilityCard<WylderWildfirePower>
{
    public WylderWildfire() : base(1, CardRarity.Uncommon) { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderBoldness : WylderAbilityCard<WylderBoldnessPower>
{
    public WylderBoldness() : base(1,CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_boldness.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Block",4m) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}
public sealed class WylderNightForm : WylderAbilityCard<WylderNightFormPower>
{
    public WylderNightForm() : base(3,CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_night_form.png";
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
public sealed class WylderRemainingEmber : WylderAbilityCard<WylderRemainingEmberPower>
{
    public WylderRemainingEmber() : base(1,CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_remaining_ember.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(
        HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(IsUpgraded));
}
public sealed class WylderEruption : WylderAbilityCard<WylderEruptionPower>
{
    public WylderEruption() : base(1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
public sealed class WylderUndying : WylderAbilityCard<WylderUndyingPower>
{
    public WylderUndying() : base(2,CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_undying.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
