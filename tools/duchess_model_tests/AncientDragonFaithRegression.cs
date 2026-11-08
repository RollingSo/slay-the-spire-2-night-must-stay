using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class AncientDragonFaithRegression
{
    private static TaskCompletionSource<IEnumerable<CardModel>> _choice = null!;
    private static PlayerChoiceContext _expected = null!;
    private static Player _owner = null!;
    private static int _selections;

    public static void Run()
    {
        var h = new HarmonyLib.Harmony("night-must-stay.tests.ancient-dragon-faith");
        var select = typeof(CardSelectCmd).GetMethods().Single(m => m.Name == nameof(CardSelectCmd.FromCombatPile)
            && m.GetParameters().Length == 4);
        h.Patch(select, prefix: new HarmonyMethod(typeof(AncientDragonFaithRegression), nameof(Select)));
        h.Patch(typeof(CardPileCmd).GetMethods().Single(m => m.Name == nameof(CardPileCmd.Add)
            && m.GetParameters()[0].ParameterType == typeof(CardModel)
            && m.GetParameters()[1].ParameterType == typeof(PileType)),
            prefix: new HarmonyMethod(typeof(AncientDragonFaithRegression), nameof(MoveToHand)));
        try
        {
            if (typeof(AncientDragonFaithPower).GetMethod("AfterSideTurnStart")!.DeclaringType == typeof(AncientDragonFaithPower))
                throw new Exception("Recovery must not wait in the shared side-turn hook.");
            foreach (int players in new[] { 2, 3, 4 })
            {
                _owner = TrackingArrowRegression.MakePlayer();
                var creature = new Creature(_owner, 70, 70);
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(_owner, creature);
                var power = ModelDb.Power<AncientDragonFaithPower>().ToMutable();
                AccessTools.PropertySetter(typeof(PowerModel), "Owner").Invoke(power, new object[] { creature });
                power.SetAmount(2, false);
                _expected = new BlockingPlayerChoiceContext(); // Identity fixture, not production scheduling.
                _selections = 0;
                power.AfterPlayerTurnStart(_expected, _owner).GetAwaiter().GetResult();
                if (_selections != 0) throw new Exception("Empty discard must not open a choice.");
                var card = ModelDb.Card<AncientDragonLightning>().ToMutable(); card.Owner = _owner;
                Cards(PileType.Discard, _owner).Add(card);
                _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Task ownerTurn = power.AfterPlayerTurnStart(_expected, _owner);
                if (ownerTurn.IsCompleted || _selections != 1) throw new Exception("Owner choice was not awaited.");
                for (int i = 1; i < players; i++)
                {
                    var ally = TrackingArrowRegression.MakePlayer();
                    if (!power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ally).IsCompletedSuccessfully)
                        throw new Exception("A teammate must not wait for the owner's pending recovery.");
                }
                if (_selections != 1) throw new Exception("Another player opened the owner's selector.");
                _choice.SetResult(new[] { card }); ownerTurn.GetAwaiter().GetResult();
                if (card.Pile?.Type != PileType.Hand) throw new Exception("Selected discard card was not recovered.");
                Cards(PileType.Hand, _owner).Remove(card); Cards(PileType.Discard, _owner).Add(card);
                _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);
                ownerTurn = power.AfterPlayerTurnStart(_expected, _owner);
                _choice.SetResult(Array.Empty<CardModel>()); ownerTurn.GetAwaiter().GetResult();
                if (card.Pile?.Type != PileType.Discard) throw new Exception("Empty selection must not move cards.");
            }
            Console.WriteLine("PASS Ancient Dragon Faith uses supplied player context, isolates owner selection for 2/3/4 players, recovers selected cards and handles empty discard/selection (UI and movement stubbed).");
        }
        finally { h.UnpatchAll(h.Id); }
    }

    private static List<CardModel> Cards(PileType pile, Player player) =>
        (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(CardPile.Get(pile, player))!;
    public static bool Select(PlayerChoiceContext context, CardPile pile, Player player, ref Task<IEnumerable<CardModel>> __result)
    {
        if (!ReferenceEquals(context, _expected) || player != _owner || pile.Type != PileType.Discard)
            throw new Exception("Recovery replaced the player context or selected the wrong pile/owner.");
        _selections++; __result = _choice.Task; return false;
    }
    public static bool MoveToHand(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
    {
        if (newPileType != PileType.Hand) throw new Exception("Recovery destination must be Hand.");
        Cards(card.Pile!.Type, card.Owner).Remove(card); Cards(newPileType, card.Owner).Add(card);
        __result = Task.FromResult(new CardPileAddResult { success = true, cardAdded = card }); return false;
    }
}
