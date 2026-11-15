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
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

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
public sealed class WylderClawPull : WylderCard
{
    public WylderClawPull() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_claw_pull.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new PowerVar<WeakPower>("Weak", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<WeakPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target?.IsAlive == true)
            await PowerCmd.Apply<WeakPower>(context, play.Target, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Weak"].UpgradeValueBy(1m);
}
public sealed class WylderParry : WylderDesperationCard
{
    public WylderParry() : base(4, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override bool IsPlayable => CombatState == null || WylderCompletionRules.AttackingEnemies(this).Length > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
        { HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!WylderCompletionRules.IsAttacking(play.Target)) return;
        WylderAnimationPatch.PlayBlock(Owner.Creature);
        WylderAttackEffects.Play(play.Target, WylderAttackVfx.Kind.BucklerImpact, Owner.Creature);
        await CreatureCmd.Stun(play.Target);
    }
    protected override IEnumerable<Creature> LegalDesperationTargets() => WylderCompletionRules.AttackingEnemies(this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
public sealed class WylderCorpseWaxCutter : WylderCard
{
    public WylderCorpseWaxCutter() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_corpse_wax_cutter.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(14m, ValueProp.Move), new DynamicVar("Break", 30m), new PowerVar<VulnerablePower>("Vulnerable", 2m), new PowerVar<WeakPower>("Weak", 2m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithWylderFx(WylderAttackVfx.Kind.CorpseWax, Owner.Creature).Execute(context);
        if (BrokeThisPlay)
        {
            var enemies = CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray();
            await PowerCmd.Apply<VulnerablePower>(context, enemies, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(context, enemies, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}
