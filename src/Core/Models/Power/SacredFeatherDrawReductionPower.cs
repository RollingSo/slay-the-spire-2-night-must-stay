using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Power;

// Same turn-start boundary as native DrawCardsNextTurnPower, but subtracts
// initial hand draw only. Additional CardPileCmd.Draw calls are unaffected.
public sealed class SacredFeatherDrawReductionPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player && AmountOnTurnStart != 0 ? Math.Max(0m, count - Amount) : count;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner) && AmountOnTurnStart != 0)
            await PowerCmd.Remove(this);
    }
}
