using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace sts2mod.Core.Models.Power
{
    public sealed class CounterStepPower : PowerModel
    {
        private sealed class Data
        {
            public bool TriggeredThisTurn;
        }

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData()
        {
            return new Data();
        }

        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side == base.Owner.Side)
            {
                GetInternalData<Data>().TriggeredThisTurn = false;
            }

            return Task.CompletedTask;
        }

        public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != base.Owner.Side)
                return;

            Data data = GetInternalData<Data>();
            if (!data.TriggeredThisTurn && GuardCounterPower.SucceededAtStartOfThisTurn(combatState, base.Owner))
            {
                data.TriggeredThisTurn = true;
                Flash();
                await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
            }
        }
    }
}
