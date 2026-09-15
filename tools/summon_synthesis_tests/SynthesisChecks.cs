using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Cards;

public static class SynthesisChecks
{
    static readonly List<NetPlayerChoiceResult> Choices = new();
    static readonly List<CardModel> Exhausted = new();
    static readonly List<PileType?> ExhaustedFrom = new();
    static CardModel? Generated;
    static CardModel? First;
    static bool Replay;
    static bool Interleave;
    static int Selections;

    public static async Task Run()
    {
        AccessTools.Method(typeof(ModManager), "ResetForTests").Invoke(null, null);
        var state = AccessTools.Property(typeof(ModManager), "State");
        state.SetValue(null, Enum.Parse(state.PropertyType, "Skipped"));
        MethodInfo init = AccessTools.Method(typeof(ModelDb), "Init");
        init.Invoke(null, init.GetParameters().Length == 0 ? null : new object[] { Type.EmptyTypes });
        foreach (Type type in new[] { typeof(DefendGuardian), typeof(StrikeGuardian), typeof(EvolvedDefend),
            typeof(SpearAndShield), typeof(UltimateDefend), typeof(ShieldPoke) })
            if (!ModelDb.Contains(type)) AccessTools.Method(typeof(ModelDb), "Inject").Invoke(null, [type]);

        var harmony = new HarmonyLib.Harmony("NightMustStay.Synthesis.Tests");
        try
        {
            Patch(harmony, typeof(CardSelectCmd).GetMethods().Single(m => m.Name == "FromCombatPile" && m.GetParameters().Length == 5), nameof(Select));
            Patch(harmony, AccessTools.Method(typeof(CardCmd), "Exhaust"), nameof(Exhaust));
            Patch(harmony, typeof(CardCmd).GetMethods().Single(m => m.Name == "Upgrade" && m.GetParameters()[0].ParameterType == typeof(CardModel)), nameof(Upgrade));
            Patch(harmony, AccessTools.Method(typeof(CardPileCmd), "AddGeneratedCardToCombat"), nameof(AddGenerated));

            foreach (Type type in new[] { typeof(EvolvedDefend), typeof(SpearAndShield) })
            foreach (bool upgraded in new[] { false, true })
            foreach (int materials in new[] { 0, 1, 2, 3 })
            {
                Choices.Clear();
                foreach (bool replay in new[] { false, true })
                {
                    Replay = replay;
                    var (card, cards) = Setup(type, upgraded, materials);
                    Interleave = false;
                    await Play(card);
                    if (materials < 2)
                        Check(Selections == 0 && Generated == null && Exhausted.Count == 0, "Insufficient materials did not return cleanly.");
                    else
                    {
                        Check(Selections == 2 && Exhausted.Count == 2, "Synthesis did not select and exhaust exactly twice.");
                        Check(ReferenceEquals(Exhausted[0], cards[1]) && ReferenceEquals(Exhausted[1], cards[0]), "Same-name materials mapped to the wrong instances.");
                        Check(ExhaustedFrom.All(p => p == PileType.Discard), "Unexpected source pile.");
                        Check(Generated?.GetType() == (type == typeof(EvolvedDefend) ? typeof(UltimateDefend) : typeof(ShieldPoke)), "Wrong synthesis output.");
                        Check(Generated!.IsUpgraded == upgraded && ReferenceEquals(Generated.Owner, card.Owner), "Output upgrade/owner diverged.");
                    }
                }
            }
            Console.WriteLine("PASS: both synthesis OnPlay methods; base/upgraded; 0/1/2/3 materials; duplicate names; native combat-ID encode/decode replay; output owner and upgrade. Selection UI and pile-command effects are stubbed, not a live multiplayer session.");

            // Optional diagnostic, not an assertion that stale material consumption is desired.
            if (Environment.GetCommandLineArgs().Contains("--audit-interleaving"))
            foreach (Type type in new[] { typeof(EvolvedDefend), typeof(SpearAndShield) })
            {
                Choices.Clear();
                Replay = false;
                var (card, _) = Setup(type, false, 3);
                Interleave = true;
                await Play(card);
                Console.WriteLine($"AUDIT {type.Name}: first material moved to Hand while second choice awaited; exhausted from {string.Join(",", ExhaustedFrom)}; generated output = {Generated != null}.");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); NetCombatCardDb.Instance.ClearCardsForTesting(); }
    }

    static (CardModel, CardModel[]) Setup(Type type, bool upgraded, int count)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var playerState = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        foreach (var (property, pile) in new[] { ("Hand", PileType.Hand), ("DrawPile", PileType.Draw),
            ("DiscardPile", PileType.Discard), ("ExhaustPile", PileType.Exhaust), ("PlayPile", PileType.Play) })
            AccessTools.Field(typeof(PlayerCombatState), $"<{property}>k__BackingField").SetValue(playerState, new CardPile(pile));
        AccessTools.Property(typeof(Player), "PlayerCombatState").SetValue(player, playerState);
        AccessTools.Field(typeof(Player), "<Deck>k__BackingField").SetValue(player, new CardPile(PileType.Deck));
        AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
        var creature = new Creature(player, 50, 50);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        var combat = new CombatState();
        AccessTools.Property(typeof(Creature), "CombatState").SetValue(creature, combat);
        CardModel Make(Type cardType, CardPile pile)
        {
            var canonical = (CardModel)typeof(ModelDb).GetMethod("Card")!.MakeGenericMethod(cardType).Invoke(null, null)!;
            CardModel card = canonical.ToMutable();
            combat.AddCard(card, player);
            pile.AddInternal(card, silent: true);
            return card;
        }
        CardModel source = Make(type, playerState.PlayPile);
        if (upgraded) AccessTools.Method(typeof(CardModel), "UpgradeInternal").Invoke(source, null);
        CardModel[] materials = Enumerable.Range(0, count).Select(i => Make(i < 2 ? typeof(DefendGuardian) : typeof(StrikeGuardian), playerState.DiscardPile)).ToArray();
        NetCombatCardDb.Instance.ClearCardsForTesting();
        // Seed the native ID maps directly to avoid its Godot-dependent debug logger.
        var byId = (Dictionary<uint, CardModel>)AccessTools.Field(typeof(NetCombatCardDb), "_idToCard").GetValue(NetCombatCardDb.Instance)!;
        var byCard = (Dictionary<CardModel, uint>)AccessTools.Field(typeof(NetCombatCardDb), "_cardToId").GetValue(NetCombatCardDb.Instance)!;
        uint id = 0;
        foreach (CardModel card in materials.Prepend(source))
        {
            byId.Add(id, card);
            byCard.Add(card, id++);
        }
        // Mimic a peer's different UI/pile order after the IDs have been assigned.
        if (Replay && materials.Length > 1)
        {
            playerState.DiscardPile.RemoveInternal(materials[0], silent: true);
            playerState.DiscardPile.AddInternal(materials[0], silent: true);
        }
        Selections = 0;
        First = Generated = null;
        Exhausted.Clear();
        ExhaustedFrom.Clear();
        return (source, materials);
    }

    static Task Play(CardModel card) => (Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, [new BlockingPlayerChoiceContext(), null])!;
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void Patch(HarmonyLib.Harmony harmony, MethodInfo method, string prefix) =>
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(SynthesisChecks), prefix));

    public static bool Select(CardPile pile, Player player, CardSelectorPrefs prefs,
        Func<CardModel, bool> filter, ref Task<IEnumerable<CardModel>> __result)
    {
        Check(prefs.MinSelect == 1 && prefs.MaxSelect == 1, "Selection count changed.");
        CardModel selected;
        if (Replay)
            selected = PlayerChoiceResult.FromNetData(player, null!, Choices[Selections]).AsCombatCards().Single();
        else
        {
            CardModel[] candidates = pile.Cards.Where(filter).ToArray();
            selected = Selections == 0 ? candidates.Last() : candidates.First();
            Choices.Add(PlayerChoiceResult.FromMutableCombatCards([selected]).ToNetData());
        }
        Check(filter(selected) && ReferenceEquals(selected.Owner, player), "Choice rejected by filter/owner.");
        Selections++;
        if (Selections == 1) First = selected;
        __result = CompleteChoice();
        return false;
        async Task<IEnumerable<CardModel>> CompleteChoice()
        {
            await Task.Yield();
            if (Interleave && Selections == 2)
            {
                First!.Pile!.RemoveInternal(First, silent: true);
                player.PlayerCombatState!.Hand.AddInternal(First, silent: true);
            }
            return new[] { selected };
        }
    }
    public static bool Exhaust(CardModel card, ref Task __result)
    {
        Exhausted.Add(card);
        ExhaustedFrom.Add(card.Pile?.Type);
        card.Pile!.RemoveInternal(card, silent: true);
        card.Owner.PlayerCombatState!.ExhaustPile.AddInternal(card, silent: true);
        __result = Task.CompletedTask;
        return false;
    }
    public static bool Upgrade(CardModel card)
    {
        AccessTools.Method(typeof(CardModel), "UpgradeInternal").Invoke(card, null);
        return false;
    }
    public static bool AddGenerated(CardModel card, PileType newPileType, Player creator, ref Task<CardPileAddResult> __result)
    {
        Check(newPileType == PileType.Hand && ReferenceEquals(creator, card.Owner), "Wrong generated-card destination.");
        Generated = card;
        __result = Task.FromResult(new CardPileAddResult());
        return false;
    }
}
