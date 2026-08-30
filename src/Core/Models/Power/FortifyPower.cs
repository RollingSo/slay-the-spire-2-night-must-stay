using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace NightMustStay.Core.Models.Power
{
    public sealed class FortifyPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override bool ShouldClearBlock(Creature creature)
        {
            if (base.Owner != creature)
                return true;
            return false;
        }

        public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
        {
            if (this != preventer || creature != base.Owner)
                return;

            int clampAllowance = base.Owner.Player?.GetRelic<SturdyClamp>() != null ? 10 : 0;
            int blockBeforeRetention = base.Owner.Block;
            Flash();
            int retainedBlock = System.Math.Min(
                blockBeforeRetention,
                (int)base.Amount + clampAllowance);
            int retainedByFortify = System.Math.Min(
                (int)base.Amount,
                System.Math.Max(0, blockBeforeRetention - clampAllowance));
            GuardianMultiplayerPower guardianPower = base.Owner.GetPower<GuardianMultiplayerPower>();
            if (guardianPower != null && retainedByFortify > 0)
                await guardianPower.GiveRetainedBlockToTeammates(retainedByFortify);
            int blockToLose = base.Owner.Block - retainedBlock;
            if (blockToLose > 0)
                await NightMustStay.Core.Compatibility.Sts2BranchCompat.LoseBlock(base.Owner, blockToLose);
            await PowerCmd.Decrement(this);
        }
    }
}
