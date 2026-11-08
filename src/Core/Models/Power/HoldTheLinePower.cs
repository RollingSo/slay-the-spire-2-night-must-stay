using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Power;

public sealed class HoldTheLinePower : PowerModel
{
    public CardPlay GrantingPlay { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block,
        ValueProp props, CardModel cardSource, CardPlay cardPlay) =>
        target == Owner && cardSource?.Owner == Owner.Player && cardPlay != GrantingPlay ? 2m : 1m;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || play == GrantingPlay) return;
        Flash();
        await PowerCmd.Remove(this);
    }
}
