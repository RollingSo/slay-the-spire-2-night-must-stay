using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

// Party-wide aura: filter the dealer, not the recipient. This also covers
// enemies summoned after the card was played, without reapplying a debuff.
public sealed class SaviorSpreadWingsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Routed through the branch-compatible Harmony hook (release/Beta have
    // different virtual signatures), just like the existing damage reduction.
    internal decimal GetAttackMultiplier(Creature dealer, ValueProp props) =>
        dealer?.Side == CombatSide.Enemy && props.IsPoweredAttack()
            ? Math.Clamp((100m - Amount) / 100m, 0m, 1m)
            : 1m;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> creatures)
    {
        // Do not expire at the end of the players' card-playing phase.
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
