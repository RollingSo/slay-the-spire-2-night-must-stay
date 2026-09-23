using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
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
        public int TurnStartEnergy;
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
        ? Math.Clamp(decimal.ToInt32(power.Amount) - 1, 0, 12)
        : 0;

    public static async Task Advance(PlayerChoiceContext context, Creature owner, int amount, AbstractModel source)
    {
        if (amount == 0) return;
        DuchessMomentPower power = await Ensure(context, owner);
        int before = Current(owner.Player);
        int target = ((before + amount) % 13 + 13) % 13;
        await Set(context, owner, target, source);
    }

    public static async Task Set(PlayerChoiceContext context, Creature owner, int moment, AbstractModel source)
    {
        DuchessMomentPower power = await Ensure(context, owner);
        int before = Current(owner.Player);
        int target = Math.Clamp(moment, 0, 12);
        int delta = target - before;
        if (delta != 0)
            await PowerCmd.Apply<DuchessMomentPower>(context, owner, delta, owner, null);
        await NotifyMomentChanged(context, owner.Player, before, target);
    }

    private static async Task NotifyMomentChanged(PlayerChoiceContext context, Player player, int before, int after)
    {
        if (before != 4 && after == 4)
        {
            foreach (DuchessOldPocketwatch relic in player.Relics.OfType<DuchessOldPocketwatch>().ToArray())
                await relic.OnMomentFour(context);
        }
        if (before != 5 && after == 5)
        {
            foreach (DuchessMomentFiveRewardPower power in player.Creature.Powers
                         .OfType<DuchessMomentFiveRewardPower>().ToArray())
                await power.OnMomentFive(context);
            foreach (DuchessBeatPower power in player.Creature.Powers.OfType<DuchessBeatPower>().ToArray())
                await power.OnMomentFive(context);
        }
        if (before != 6 && after == 6)
            foreach (DuchessFutureMomentEnergyPower power in player.Creature.Powers
                         .OfType<DuchessFutureMomentEnergyPower>().ToArray())
                await power.OnMomentSix(context);
        if (before != 12 && after == 12)
            foreach (DuchessEternalRestagePower power in player.Creature.Powers
                         .OfType<DuchessEternalRestagePower>().ToArray())
                await power.OnMomentTwelve(context);
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
            return;

        Data data = GetInternalData<Data>();
        data.PendingMoment = null;
        await Set(context, Owner, 0, this);
        data.TurnStartEnergy = player.PlayerCombatState.Energy;
        data.TurnStart.Clear();
        foreach (CardPile pile in player.PlayerCombatState.AllPiles)
        {
            for (int i = 0; i < pile.Cards.Count; i++)
            {
                CardModel card = pile.Cards[i];
                data.TurnStart[card] = new CardSnapshot(pile.Type, i, card.EnergyCost.GetResolved());
            }
        }
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
            // The engine dispatches this hook once per CardPlay, including
            // each native Replay instance, so every actual play advances once.
            await Advance(context, Owner, 1, this);
        }
    }

    // Let the engine's native CardPlay loop execute Replay. This hook only
    // contributes the replay count; the card's OnPlay runs through the normal
    // native replay path, including history, targets, and moment accounting.
    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
    {
        if (card.Owner?.Creature == Owner && card is DuchessCard duchessCard
            && duchessCard.IsMomentActive
            && duchessCard.Spec.Effects.Any(effect => effect.Kind == "Replay"))
            return playCount + 1;

        return playCount;
    }

    public void SetAfterCurrentCard(int moment) => GetInternalData<Data>().PendingMoment = Math.Clamp(moment, 0, 12);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not DuchessCard duchessCard || card.Owner?.Creature != Owner)
            return false;
        int discount = 0;
        if (duchessCard.IsMomentActive)
            discount += duchessCard.Spec.MomentCostReduction;
        if (duchessCard.Spec.MomentCostReductionDynamic)
            discount += Current(Owner.Player);
        if (duchessCard.Spec.ConcealedCostReduction > 0
            && Owner.HasPower<DuchessConcealmentPower>())
            discount += duchessCard.Spec.ConcealedCostReduction;
        modifiedCost = Math.Max(0m, originalCost - discount);
        return modifiedCost != originalCost;
    }

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
        await PlayerCmd.SetEnergy(data.TurnStartEnergy, Owner.Player);
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

public sealed class DuchessRadiantBladeTurnsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player != Owner.Player)
            return;
        DuchessRadiantBlade blade = combatState.CreateCard<DuchessRadiantBlade>(player);
        if (player.Creature.HasPower<DuchessRadiantBladeGrowthPower>())
            blade.DynamicVars.Damage.BaseValue += player.Creature.GetPower<DuchessRadiantBladeGrowthPower>().TotalGrowth;
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
            blade, PileType.Hand, player, CardPilePosition.Top);
        CardCmd.PreviewCardPileAdd(result);
        await PowerCmd.Decrement(this);
    }
}

public sealed class DuchessTemporaryStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DuchessMagicDagger>();
    protected override bool IsPositive => false;
}

public sealed class DuchessTurnStartSwapPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStartLate(
        CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner)) return;
        Player player = Owner.Player;
        CardPile hand = PileType.Hand.GetPile(player);
        for (int i = 0; i < decimal.ToInt32(Amount) && hand.Cards.Count > 0; i++)
        {
            CardModel selected = (await CardSelectCmd.FromCombatPile(
                new BlockingPlayerChoiceContext(), hand, player,
                new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_TURN_START_SWAP"), 1)))
                .FirstOrDefault();
            if (selected == null) return;
            Flash();
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Random, this);
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1m, player);
        }
    }
}

public sealed class DuchessNextTurnEnergyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player != Owner.Player) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }
}

public sealed class DuchessNextTurnDrawPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player != Owner.Player) return;
        Flash();
        await CardPileCmd.Draw(context, Amount, player);
        await PowerCmd.Remove(this);
    }
}

public sealed class DuchessFutureMomentEnergyPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public async Task OnMomentSix(PlayerChoiceContext context)
    {
        // Remove first: gaining energy can dispatch more hooks before this
        // callback returns, and this reward is strictly one-shot.
        decimal reward = Amount;
        Flash();
        await PowerCmd.Remove(this);
        await PlayerCmd.GainEnergy(reward, Owner.Player);
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}

public sealed class DuchessEndTurnMomentBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        int moment = DuchessMomentPower.Current(Owner.Player);
        if (moment <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount * moment, ValueProp.Unpowered, null);
    }
}

public sealed class DuchessReactionBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner?.Creature != Owner || play.Card is not DuchessCard { HasReaction: true }) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

public sealed class DuchessReactionDrawPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card.Owner?.Creature != Owner || oldPileType != PileType.Draw
            || card.Pile?.Type != PileType.Hand || card is not DuchessCard { HasReaction: true })
            return;
        Flash();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), Amount, Owner.Player);
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
