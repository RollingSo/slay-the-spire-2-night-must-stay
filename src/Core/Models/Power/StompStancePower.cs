using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using sts2mod.Core.Models;

namespace sts2mod.Core.Models.Power
{
    public sealed class StompStancePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
        {
            if (card.Owner.Creature != base.Owner)
                return playCount;

            if (GuardianCardFilters.HasDefendInName(card))
                return playCount + 1;

            return playCount;
        }

        public override async Task AfterModifyingCardPlayCount(CardModel card)
        {
            if (card.Owner.Creature == base.Owner && GuardianCardFilters.HasDefendInName(card))
            {
                Flash();
                await PowerCmd.Decrement(this);
            }
        }

    }
}
