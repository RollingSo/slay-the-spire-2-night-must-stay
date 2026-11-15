using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Models.Cards;

internal static class WylderHandPlayContext
{
    private sealed class Wrapper
    {
        internal bool FromHand;
        internal int Count;
        internal int Vigor;
        internal bool Consumed;
    }
    internal sealed class Snapshot
    {
        internal bool FromHand;
        internal int Count;
        internal int Vigor;
    }
    private static readonly ConditionalWeakTable<CardModel, Wrapper> Wrappers = new();
    private static readonly ConditionalWeakTable<CardPlay, Snapshot> Plays = new();

    // Capture BEFORE the engine moves the card from Hand into Play. IsAutoPlay
    // is not a substitute: the end-turn Parry is also played from Hand.
    internal static void CaptureWrapper(CardModel card)
    {
        if (!card.IsMutable || card.Owner?.Character is not Wylder) return;
        var data = Wrappers.GetOrCreateValue(card);
        data.FromHand = card.Pile?.Type == PileType.Hand;
        data.Count = data.FromHand ? PileType.Hand.GetPile(card.Owner).Cards.Count : 0;
        data.Vigor = card.Owner.Creature.GetPowerAmount<VigorPower>();
        data.Consumed = false;
    }
    internal static void Record(CardPlay play)
    {
        if (!play.Card.IsMutable || play.Card.Owner?.Character is not Wylder || Plays.TryGetValue(play, out _)) return;
        var snapshot = new Snapshot();
        if (Wrappers.TryGetValue(play.Card, out var wrapper))
        {
            snapshot.FromHand = !wrapper.Consumed && play.IsFirstInSeries && wrapper.FromHand;
            snapshot.Count = snapshot.FromHand ? wrapper.Count : 0;
            snapshot.Vigor = snapshot.FromHand ? wrapper.Vigor : 0;
            wrapper.Consumed = true;
        }
        Plays.Add(play, snapshot);
    }
    internal static int? HandCount(CardModel card, CardPlay play)
    {
        if (!card.IsMutable || card.Owner == null) return null;
        if (play != null)
            return Plays.TryGetValue(play, out var snapshot) && snapshot.FromHand ? snapshot.Count : null;
        return card.Pile?.Type == PileType.Hand ? PileType.Hand.GetPile(card.Owner).Cards.Count : null;
    }
    internal static bool IsLowHand(CardModel card, CardPlay play, int threshold) =>
        HandCount(card, play) is int count && count <= threshold;
    internal static int? SourceVigor(CardModel card, CardPlay play)
    {
        if (!card.IsMutable || card.Owner == null) return null;
        if (play != null) return Plays.TryGetValue(play, out var s) && s.FromHand ? s.Vigor : null;
        return card.Pile?.Type == PileType.Hand ? card.Owner.Creature.GetPowerAmount<VigorPower>() : null;
    }
}
