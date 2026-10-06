using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

internal static class TrackingArrowRegression
{
    private static readonly List<CardModel> Returned = new();

    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.tracking-arrow");
        harmony.Patch(typeof(CardPileCmd).GetMethods().Single(method => method.Name == "Add"
            && method.GetParameters()[0].ParameterType == typeof(CardModel)
            && method.GetParameters()[1].ParameterType == typeof(PileType)),
            prefix: new HarmonyMethod(typeof(TrackingArrowRegression), nameof(CaptureReturn)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                Returned.Clear();
                Player owner = MakePlayer();
                Player ally = MakePlayer();
                TrackingArrow discard = MakeArrow(owner, PileType.Discard, upgraded);
                TrackingArrow draw = MakeArrow(owner, PileType.Draw, upgraded);
                TrackingArrow hand = MakeArrow(owner, PileType.Hand, upgraded);
                TrackingArrow resolving = MakeArrow(owner, PileType.Play, upgraded);
                TrackingArrow allyArrow = MakeArrow(ally, PileType.Discard, upgraded);
                TrackingArrow deck = MakeArrow(owner, PileType.Deck, upgraded);
                TrackingArrow.ReturnAllAfterMarkTrigger(null!, owner).GetAwaiter().GetResult();
                Assert(Returned.Count == 2 && Returned.Contains(discard) && Returned.Contains(draw),
                    "Another card's Mark must return all eligible owner copies, not ally/hand/deck/resolving cards.");
                Assert(resolving.Pile!.Type == PileType.Play, "Resolving card must wait for native cleanup.");
                Move(resolving, PileType.Discard);
                var play = new CardPlay { Card = resolving, Player = owner, Target = null, ResultPile = PileType.Discard,
                    Resources = default!, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
                resolving.AfterCardPlayed(null!, play).GetAwaiter().GetResult();
                Assert(Returned.Count == 3 && Returned.Contains(resolving), "Own Mark must still return after cleanup.");
                resolving.AfterCardPlayed(null!, play).GetAwaiter().GetResult();
                TrackingArrow.ReturnAllAfterMarkTrigger(null!, owner).GetAwaiter().GetResult();
                Assert(Returned.Count == 3 && allyArrow.Pile!.Type == PileType.Discard
                    && deck.Pile!.Type == PileType.Deck && hand.Pile!.Type == PileType.Hand,
                    "Repeated Mark must not duplicate returned cards or affect another player/permanent deck.");
            }
            Console.WriteLine("PASS: normal/upgraded Tracking Arrows return on owner's external Mark; multiple copies, own-play deferral, repeated triggers, hand/deck exclusion and multiplayer ownership (movement visuals stubbed).");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    internal static Player MakePlayer()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var combat = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        Set(player, "<Deck>k__BackingField", new CardPile(PileType.Deck));
        Set(player, "<PlayerCombatState>k__BackingField", combat);
        foreach (var (name, type) in new[] { ("Hand", PileType.Hand), ("DrawPile", PileType.Draw),
            ("DiscardPile", PileType.Discard), ("ExhaustPile", PileType.Exhaust), ("PlayPile", PileType.Play) })
            Set(combat, "<" + name + ">k__BackingField", new CardPile(type));
        return player;
    }

    private static TrackingArrow MakeArrow(Player owner, PileType type, bool upgraded)
    {
        var arrow = (TrackingArrow)ModelDb.Card<TrackingArrow>().ToMutable();
        Set(arrow, "_owner", owner, typeof(CardModel));
        if (upgraded) arrow.UpgradeInternal();
        ((List<CardModel>)typeof(CardPile).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(CardPile.Get(type, owner))!).Add(arrow);
        return arrow;
    }

    private static void Move(CardModel card, PileType type)
    {
        var oldCards = (List<CardModel>)typeof(CardPile).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card.Pile)!;
        oldCards.Remove(card);
        ((List<CardModel>)typeof(CardPile).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(CardPile.Get(type, card.Owner))!).Add(card);
    }

    public static bool CaptureReturn(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
    {
        Assert(newPileType == PileType.Hand, "Tracking Arrow must return to Hand.");
        Returned.Add(card);
        Move(card, newPileType);
        __result = Task.FromResult(new CardPileAddResult { success = true, cardAdded = card });
        return false;
    }

    private static void Set(object target, string name, object value, Type? type = null) =>
        (type ?? target.GetType()).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
}
