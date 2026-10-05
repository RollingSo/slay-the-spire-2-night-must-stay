using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Power
{
    public sealed class FortifyPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public int GetAutomaticClearRetainedBlock(int currentBlock)
        {
            return System.Math.Min(System.Math.Max(0, currentBlock), System.Math.Max(0, Amount));
        }

        public void NotifyAutomaticClearRetention(int retainedBlock)
        {
            if (retainedBlock <= 0) return;
            Flash();
            Owner.GetPower<GuardianMultiplayerPower>()?.QueueRetainedBlockForTeammates(retainedBlock);
        }

        public override async Task AfterSideTurnStart(CombatSide side,
            IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != Owner.Side || !creatures.Contains(Owner)) return;
            await PowerCmd.Decrement(this);
        }
    }
}
