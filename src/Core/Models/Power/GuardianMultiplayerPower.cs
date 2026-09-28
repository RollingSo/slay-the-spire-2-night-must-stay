using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power
{
    public sealed class GuardianMultiplayerPower : PowerModel
    {
        private decimal _pendingTeammateBlock;

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public void QueueRetainedBlockForTeammates(decimal retainedBlock)
        {
            if (retainedBlock <= 0m || Owner.CombatState == null)
                return;

            Flash();
            _pendingTeammateBlock += retainedBlock * Amount;
        }

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != Owner.Side || !creatures.Contains(Owner) ||
                _pendingTeammateBlock <= 0m)
            {
                return;
            }

            // Creature.AfterTurnStart clears each creature's Block one at a
            // time. Granting this Block from Fortify's retention callback can
            // therefore be erased when a teammate later in that loop clears
            // their Block. AfterSideTurnStart runs after the entire clear loop.
            decimal amount = _pendingTeammateBlock;
            _pendingTeammateBlock = 0m;
            foreach (var teammate in Owner.CombatState.Players.Where(player =>
                         player.Creature != Owner && player.Creature.IsAlive))
            {
                await CreatureCmd.GainBlock(
                    teammate.Creature,
                    amount,
                    ValueProp.Unpowered,
                    null);
            }
        }
    }
}
