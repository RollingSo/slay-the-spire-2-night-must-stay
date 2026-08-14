using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace sts2mod.Core.Models.Power
{
    public sealed class WingFlapPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
        {
            if (amount <= 0m
                || applier != base.Owner
                || power is not WeakPower
                || power.Owner.Side == base.Owner.Side)
            {
                return;
            }

            Flash();
            await CreatureCmd.Damage(
                choiceContext,
                power.Owner,
                base.Amount * amount,
                ValueProp.Unpowered,
                base.Owner,
                null);
        }
    }
}
