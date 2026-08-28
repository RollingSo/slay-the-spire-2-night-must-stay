using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power
{
    public sealed class GuardianMultiplayerPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public async Task GiveRetainedBlockToTeammates(decimal retainedBlock)
        {
            if (retainedBlock <= 0m || Owner.CombatState == null)
                return;

            Flash();
            decimal amount = retainedBlock * Amount;
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

