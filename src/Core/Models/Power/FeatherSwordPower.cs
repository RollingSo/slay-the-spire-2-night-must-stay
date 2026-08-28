using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NightMustStay.Core.Models.Power
{
    public sealed class FeatherSwordPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            CardModel card = cardPlay.Card;
            if (card.Owner.Creature == base.Owner && GuardianCardFilters.HasDefendInName(card))
            {
                Flash();
                await PowerCmd.Apply<GuardCounterPower>(context, base.Owner, base.Amount, base.Owner, card);
            }
        }
    }
}
