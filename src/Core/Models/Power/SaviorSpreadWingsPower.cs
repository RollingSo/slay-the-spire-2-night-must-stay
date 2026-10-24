using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

// Enemy-side debuff: only this creature's attacks are reduced, regardless
// of which player or summon receives them.
public sealed class SaviorSpreadWingsPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Routed through the branch-compatible Harmony hook (release/Beta have
    // different virtual signatures), just like the existing damage reduction.
    internal decimal GetAttackMultiplier(Creature dealer, ValueProp props) =>
        dealer == Owner && dealer?.Side == CombatSide.Enemy && props.IsPoweredAttack()
            ? Math.Clamp((100m - Amount) / 100m, 0m, 1m)
            : 1m;

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature applier, CardModel cardSource) =>
        power == this ? RefreshEnemyIntent() : Task.CompletedTask;

    public override Task AfterRemoved(Creature remover) => RefreshEnemyIntent();

    private async Task RefreshEnemyIntent()
    {
        // The status is enemy-owned. Recalculate its intention for each local
        // recipient after adding/changing/removing the outgoing multiplier;
        // multiplayer clients must not keep an intention from before application.
        if (TestMode.IsOn || Owner == null || !Owner.IsAlive || Owner.CombatState == null)
            return;
        var node = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (node != null)
            await node.RefreshIntents();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> creatures)
    {
        // Do not expire at the end of the players' card-playing phase.
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
