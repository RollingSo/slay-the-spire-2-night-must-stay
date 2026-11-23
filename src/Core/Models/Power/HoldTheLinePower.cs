#nullable enable

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
    private CardPlay? _triggeringPlay;

    public CardPlay? GrantingPlay { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block,
        ValueProp props, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner && cardSource?.Owner == Owner.Player && props.IsCardOrMonsterMove()
        && (cardPlay == null || cardPlay != GrantingPlay)
        && (_triggeringPlay == null || cardPlay == _triggeringPlay) ? 2m : 1m;

    public override Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
    {
        // The engine invokes this hook for modifiers used by an actual Block gain.
        // Like Vambrace, keep the multiplier for the rest of the triggering play.
        if (modifiedAmount > 0m && cardPlay != null && _triggeringPlay == null)
        {
            _triggeringPlay = cardPlay;
            Flash();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play == _triggeringPlay)
            await PowerCmd.Remove(this);
    }
}
