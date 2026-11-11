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
