using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace sts2mod.Core.Models.Power
{
    public sealed class SaviorFormPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != base.Owner.Side)
                return;

            FortifyPower fortify = base.Owner.GetPower<FortifyPower>();
            if (fortify == null || fortify.Amount <= 0)
                return;

            Flash();
            await CreatureCmd.GainBlock(base.Owner, fortify.Amount * base.Amount, ValueProp.Unpowered, null);
        }
    }
}
