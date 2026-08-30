using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

public sealed class FreezePower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side || !creatures.Contains(Owner))
            return;

        if (Amount <= 1m)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(context, this, -1m, Applier, null);
    }
}

public sealed class HaloReturnPower : PowerModel
{
    private sealed class Data
    {
        public readonly List<CardModel> Cards = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    // This is an implementation detail used only to remember which card must
    // return next turn.  Keeping it out of the power bar also prevents the UI
    // from looking up a player-facing icon/localization entry that does not
    // belong there.
    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        if (cardSource != null && !GetInternalData<Data>().Cards.Contains(cardSource))
            GetInternalData<Data>().Cards.Add(cardSource);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner))
            return;

        foreach (CardModel card in GetInternalData<Data>().Cards.ToArray())
        {
            if (card?.Pile != null && card.Pile.Type.IsCombatPile())
            {
                if (card is Halo halo)
                    halo.IncreaseDamageForCurrentCombat();
                else if (card is ThreefoldHalo threefoldHalo)
                    threefoldHalo.ReduceCostForCurrentCombat();
                else if (card is RadagonHalo radagonHalo)
                    radagonHalo.DoubleDamageForCurrentCombat();

                if (card.Pile.Type != PileType.Hand)
                    await CardPileCmd.Add(card, PileType.Hand);
            }
        }

        await PowerCmd.Remove(this);
    }
}

/// <summary>
/// Remembers cards played on the Revenant to activate Charge. The card follows
/// the normal play flow into the discard pile, so it can be recovered early;
/// at the start of the next turn it is recovered from discard if it is there.
/// Moving it with CardPileCmd deliberately fires the normal discard-to-hand
/// hooks used by every Recover payoff.
/// </summary>
public sealed class ChargeReturnPower : PowerModel
{
    private sealed class Data
    {
        public readonly List<CardModel> Cards = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        if (cardSource != null && !GetInternalData<Data>().Cards.Contains(cardSource))
            GetInternalData<Data>().Cards.Add(cardSource);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner))
            return;

        foreach (CardModel card in GetInternalData<Data>().Cards.ToArray())
        {
            if (card?.Pile?.Type == PileType.Discard)
                await CardPileCmd.Add(card, PileType.Hand);
        }

        await PowerCmd.Remove(this);
    }
}
