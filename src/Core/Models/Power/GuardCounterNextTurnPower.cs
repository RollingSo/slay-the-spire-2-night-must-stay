using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Power
{
    public sealed class GuardCounterNextTurnPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower(ModelDb.Power<GuardCounterPower>())
        };

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != base.Owner.Side)
                return;

            Flash();
            await PowerCmd.Apply<GuardCounterPower>(new BlockingPlayerChoiceContext(), base.Owner, base.Amount, base.Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}
