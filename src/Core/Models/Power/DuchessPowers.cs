using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.Power;

// Hover-only glossary models. They are never applied to a creature.
public sealed class DuchessMomentDescriptionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

public sealed class DuchessReactionDescriptionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

public sealed class DuchessMomentPower : PowerModel
{
    private sealed record CardSnapshot(PileType Pile, int Index, int Cost);

    private sealed class Data
    {
        public readonly Dictionary<CardModel, CardSnapshot> TurnStart = new();
        public int? PendingMoment;
    }

    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public static async Task<DuchessMomentPower> Ensure(PlayerChoiceContext context, Creature owner)
    {
        if (!owner.HasPower<DuchessMomentPower>())
            await PowerCmd.Apply<DuchessMomentPower>(context, owner, 1m, owner, null);
        return owner.GetPower<DuchessMomentPower>();
    }

    public static int Current(Player player) => player?.Creature?.GetPower<DuchessMomentPower>() is { } power
        ? decimal.ToInt32(power.Amount)
        : 0;

    public static async Task Advance(PlayerChoiceContext context, Creature owner, int amount, AbstractModel source)
    {
        if (amount == 0) return;
        DuchessMomentPower power = await Ensure(context, owner);
        int before = decimal.ToInt32(power.Amount);
        await PowerCmd.Apply<DuchessMomentPower>(context, owner, amount, owner, null);
        await NotifyMomentChanged(context, owner.Player, before, Current(owner.Player));
    }

    public static async Task Set(PlayerChoiceContext context, Creature owner, int moment, AbstractModel source)
    {
        DuchessMomentPower power = await Ensure(context, owner);
        int before = decimal.ToInt32(power.Amount);
        int target = Math.Max(1, moment);
        int delta = target - before;
        if (delta != 0)
            await PowerCmd.Apply<DuchessMomentPower>(context, owner, delta, owner, null);
        await NotifyMomentChanged(context, owner.Player, before, target);
    }

    private static async Task NotifyMomentChanged(PlayerChoiceContext context, Player player, int before, int after)
    {
        if (before == 5 || after != 5) return;
        foreach (DuchessOldPocketwatch relic in player.Relics.OfType<DuchessOldPocketwatch>().ToArray())
            await relic.OnMomentFive(context);
        foreach (DuchessMomentFiveRewardPower power in player.Creature.Powers
                     .OfType<DuchessMomentFiveRewardPower>().ToArray())
            await power.OnMomentFive(context);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player == Owner.Player)
            await Set(context, Owner, 1, this);
    }

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
            return Task.CompletedTask;

        Data data = GetInternalData<Data>();
        data.PendingMoment = null;
        data.TurnStart.Clear();
        foreach (CardPile pile in player.PlayerCombatState.AllPiles)
        {
            for (int i = 0; i < pile.Cards.Count; i++)
            {
                CardModel card = pile.Cards[i];
                data.TurnStart[card] = new CardSnapshot(pile.Type, i, card.EnergyCost.GetResolved());
            }
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner?.Creature != Owner)
            return;

        Data data = GetInternalData<Data>();
        if (data.PendingMoment is int pending)
        {
            data.PendingMoment = null;
            await Set(context, Owner, pending, this);
        }
        else
        {
            await Advance(context, Owner, 1, this);
        }
    }

    public void SetAfterCurrentCard(int moment) => GetInternalData<Data>().PendingMoment = Math.Max(1, moment);

    public async Task RestoreTurnStart(PlayerChoiceContext context, CardModel source)
    {
        Data data = GetInternalData<Data>();
        if (data.TurnStart.Count == 0)
            return;

        CardModel[] generatedSinceStart = Owner.Player.PlayerCombatState.AllCards
            .Where(card => card != source && !data.TurnStart.ContainsKey(card))
            .ToArray();
        if (generatedSinceStart.Length > 0)
            await CardPileCmd.RemoveFromCombat(generatedSinceStart, false);

        foreach (IGrouping<PileType, KeyValuePair<CardModel, CardSnapshot>> group in data.TurnStart
                     .Where(pair => pair.Key != source && Owner.Player.PlayerCombatState.AllCards.Contains(pair.Key))
                     .GroupBy(pair => pair.Value.Pile))
        {
            foreach (var pair in group.OrderBy(pair => pair.Value.Index))
            {
                await CardPileCmd.Add(pair.Key, group.Key, CardPilePosition.Bottom, source, false);
                pair.Key.EnergyCost.SetThisTurn(pair.Value.Cost, true);
            }
        }
    }

    public static decimal DamageDealtThisTurn(CardModel card)
    {
        if (card.Owner == null || card.CombatState == null || CombatManager.Instance == null)
            return 0m;
        return CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(entry => entry.HappenedThisTurn(card.CombatState)
                && entry.Dealer == card.Owner.Creature
                && entry.Receiver.Side != card.Owner.Creature.Side)
            .Sum(entry => entry.Result.UnblockedDamage);
    }
}

public sealed class DuchessDodgeAtTurnStartPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player != Owner.Player)
            return;
        for (int i = 0; i < Amount; i++)
        {
            DuchessDodge dodge = combatState.CreateCard<DuchessDodge>(player);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                dodge, PileType.Draw, player, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}

public sealed class DuchessReactionBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnReactionTriggered()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

public abstract class DuchessMomentFiveRewardPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnMomentFive(PlayerChoiceContext context)
    {
        Flash();
        await ResolveMomentFive(context);
    }

    protected abstract Task ResolveMomentFive(PlayerChoiceContext context);
}

public sealed class DuchessMomentFiveBlockPower : DuchessMomentFiveRewardPower
{
    protected override Task ResolveMomentFive(PlayerChoiceContext context) =>
        CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
}

public sealed class DuchessMomentFiveDrawPower : DuchessMomentFiveRewardPower
{
    protected override Task ResolveMomentFive(PlayerChoiceContext context) => CardPileCmd.Draw(context, Amount, Owner.Player);
}

public sealed class DuchessDodgeMomentPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner?.Creature == Owner && play.Card is DuchessDodge)
        {
            Flash();
            await DuchessMomentPower.Advance(context, Owner, decimal.ToInt32(Amount), this);
        }
    }
}

public sealed class DuchessShuffleBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card.Owner?.Creature != Owner || card.Pile?.Type != PileType.Draw
            || oldPileType is not (PileType.Hand or PileType.Discard))
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
