using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using NightMustStay.Core.Models.Cards;

internal static class SoulDepartureRegression
{
    private sealed record Choice(HookPlayerChoiceContext Context, Player Player,
        CardPile Pile, TaskCompletionSource<IEnumerable<CardModel>> Completion);
    private static readonly List<Choice> Choices = new();
    private static readonly List<CardModel> Exhausted = new();

    public static void Run()
    {
        var h = new HarmonyLib.Harmony("night-must-stay.tests.soul-departure");
        h.Patch(AccessTools.PropertyGetter(typeof(CardModel), "SelectionScreenPrompt"),
            prefix: new HarmonyMethod(typeof(SoulDepartureRegression), nameof(Prompt)));
        h.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatStateFixture)));
        h.Patch(typeof(CardSelectCmd).GetMethods().Single(m => m.Name == nameof(CardSelectCmd.FromCombatPile)
                && m.GetParameters().Length == 4),
            prefix: new HarmonyMethod(typeof(SoulDepartureRegression), nameof(Select)));
        h.Patch(typeof(CardCmd).GetMethods().Single(m => m.Name == nameof(CardCmd.Exhaust)
                && m.GetParameters()[0].ParameterType == typeof(PlayerChoiceContext)
                && m.GetParameters()[1].ParameterType == typeof(CardModel)),
            prefix: new HarmonyMethod(typeof(SoulDepartureRegression), nameof(Exhaust)));
        try
        {
            foreach (int players in new[] { 2, 3, 4 })
            foreach (bool upgraded in new[] { false, true })
            foreach (bool skipRecipients in new[] { false, true })
            {
                Choices.Clear(); Exhausted.Clear();
                var combat = new CombatState();
                var participants = new List<Player>();
                var allies = (List<Creature>)AccessTools.Field(typeof(CombatState), "_allies").GetValue(combat)!;
                for (int i = 0; i < players; i++)
                {
                    var player = TrackingArrowRegression.MakePlayer();
                    AccessTools.Field(typeof(Player), "<NetId>k__BackingField").SetValue(player, (ulong)(i + 1));
                    var creature = new Creature(player, 70, 70) { CombatState = combat };
                    AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
                    participants.Add(player);
                    allies.Add(creature);
                    if (skipRecipients && i == players - 1) continue; // Empty draw pile.
                    if (skipRecipients && i == 1 && players > 2)
                        AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp)).Invoke(creature, new object[] { 0 });
                    for (int j = 0; j < 3; j++)
                    {
                        var card = ModelDb.Card<StrikeRevenant>().ToMutable(); card.Owner = player;
                        Cards(PileType.Draw, player).Add(card);
                    }
                }
                var source = ModelDb.Card<SoulDeparture>().ToMutable(); source.Owner = participants[0];
                if (upgraded) source.UpgradeInternal();
                var play = TrackingArrowRegression.MakePlay(source, source.Owner);
                // The old implementation manually paused this context and
                // waited for everybody. Neither is allowed during handoff.
                var original = new ThrowingPlayerChoiceContext();
                Task execution = (Task)AccessTools.Method(typeof(SoulDeparture), "OnPlay")
                    .Invoke(source, new object[] { original, play })!;
                execution.GetAwaiter().GetResult();
                int eligible = participants.Count(p => p.Creature.IsAlive && Cards(PileType.Draw, p).Count > 0);
                if (Choices.Count != eligible || Choices.Select(c => c.Context).Distinct().Count() != eligible)
                    throw new Exception("Every eligible player needs one independent native choice context.");
                if (Choices.Any(c => c.Context.Task == null || c.Context.Task.IsCompleted))
                    throw new Exception("Original card must finish while native recipient tasks remain pending.");
                foreach (Choice choice in Choices.AsEnumerable().Reverse())
                {
                    int count = upgraded ? 2 : 1;
                    CardModel[] selected = choice.Pile.Cards.Take(count).ToArray();
                    choice.Completion.SetResult(selected);
                    choice.Context.Task!.GetAwaiter().GetResult();
                    if (choice.Context.LastInvolvedModel != null || selected.Any(c => c.Pile?.Type != PileType.Exhaust))
                        throw new Exception("Selection must finish, exhaust its owner's cards and release its model context.");
                }
                if (Exhausted.Count != eligible * (upgraded ? 2 : 1))
                    throw new Exception("Recipient choices did not exhaust the expected number of cards.");
            }
            Console.WriteLine("PASS Soul Departure base/upgrade, 2/3/4 players, native task handoff before choices finish, per-player context ownership, reverse completion order, dead/empty recipients and exhaust ownership (queue pause, UI and exhaust visuals stubbed).");
        }
        finally { h.UnpatchAll(h.Id); }
    }

    private static List<CardModel> Cards(PileType type, Player owner) =>
        (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(CardPile.Get(type, owner))!;

    public static bool Prompt(ref LocString __result)
    { __result = new LocString("cards", "SOUL_DEPARTURE.selectionScreenPrompt"); return false; }

    public static bool Select(PlayerChoiceContext context, CardPile pile, Player player,
        ref Task<IEnumerable<CardModel>> __result)
    {
        if (context is not HookPlayerChoiceContext hook || hook.Owner != player
            || pile != CardPile.Get(PileType.Draw, player))
            throw new Exception("Recipient selection is not scoped to the correct native player queue.");
        var completion = new TaskCompletionSource<IEnumerable<CardModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Choices.Add(new(hook, player, pile, completion));
        // Model the native pause notification; use the REAL AssignTask method
        // to verify OnPlay doesn't await the unfinished selection task.
        var paused = (TaskCompletionSource)AccessTools.Field(typeof(HookPlayerChoiceContext), "_pausedCompletionSource").GetValue(hook)!;
        paused.TrySetResult();
        __result = completion.Task; return false;
    }

    public static bool Exhaust(object[] __args, ref Task __result)
    {
        CardModel card = __args.OfType<CardModel>().Single();
        var context = __args.OfType<PlayerChoiceContext>().Single();
        if (!Choices.Any(c => c.Player == card.Owner && ReferenceEquals(c.Context, context)))
            throw new Exception("Exhaust ran on another player's action context.");
        Cards(card.Pile!.Type, card.Owner).Remove(card); Cards(PileType.Exhaust, card.Owner).Add(card);
        Exhausted.Add(card); __result = Task.CompletedTask; return false;
    }
}
