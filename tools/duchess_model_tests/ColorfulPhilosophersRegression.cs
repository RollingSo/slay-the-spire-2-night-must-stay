using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using NightMustStay.Core.Models.Characters;

internal static class ColorfulPhilosophersRegression
{
    private const string OptionPrefix = "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.";
    private static CardPoolModel[] _unlockedPools = Array.Empty<CardPoolModel>();
    private static (Player? Owner, CardPoolModel? Pool) _reward;

    internal static void Run()
    {
        foreach (Type type in typeof(CardModel).Assembly.GetTypes().Where(type => !type.IsAbstract
            && (typeof(CharacterModel).IsAssignableFrom(type) || typeof(CardPoolModel).IsAssignableFrom(type)
                || type == typeof(ColorfulPhilosophers)) && type.GetConstructor(Type.EmptyTypes) != null))
            if (!ModelDb.Contains(type)) AccessTools.Method(typeof(ModelDb), "Inject").Invoke(null, new object[] { type });

        CharacterModel[] nightfarers =
        {
            ModelDb.Character<Guardian>(), ModelDb.Character<Ironeye>(), ModelDb.Character<Revenant>(),
            ModelDb.Character<Duchess>(), ModelDb.Character<Wylder>()
        };
        var poolGetter = AccessTools.PropertyGetter(typeof(ColorfulPhilosophers), "CardPoolColorOrder");
        var originalPools = ((IEnumerable<CardPoolModel>)poolGetter.Invoke(null, null)!).ToArray();
        _unlockedPools = originalPools.Concat(nightfarers.Select(character => character.CardPool)).ToArray();

        var harmony = new Harmony("NightMustStay.ColorfulPhilosophers.Regression");
        harmony.CreateClassProcessor(typeof(Guardian).Assembly.GetType(
            "NightMustStay.Core.Patches.ColorfulPhilosophersPatch", throwOnError: true)!).Patch();
        Patch(AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllEncounters)), nameof(EmptyEncounterCatalog));
        Patch(AccessTools.PropertyGetter(typeof(UnlockState), nameof(UnlockState.CharacterCardPools)), nameof(Unlocks));
        Patch(AccessTools.Method(typeof(LocString), nameof(LocString.Exists), new[] { typeof(string), typeof(string) }), nameof(Exists));
        Patch(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText)), nameof(Format));
        Patch(AccessTools.Method(typeof(ColorfulPhilosophers), "OfferRewards"), nameof(RecordRewards));
        try
        {
            var patchedPools = (IEnumerable<CardPoolModel>)poolGetter.Invoke(null, null)!;
            Check(patchedPools.SequenceEqual(_unlockedPools), "Vanilla order followed by all five Nightfarers exactly once");

            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                using var events = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/events.json"));
                foreach (var character in nightfarers)
                {
                    string key = OptionPrefix + character.CardPool.EnergyColorName.ToUpperInvariant();
                    Check(!string.IsNullOrWhiteSpace(events.RootElement.GetProperty(key + ".title").GetString())
                        && events.RootElement.GetProperty(key + ".description").GetString()!.Contains("{Cards}"),
                        $"{locale} {key} has a color name and native reward count");
                }
            }

            var owner = PlayerFor(ModelDb.Character<Ironclad>());
            RunStateFor(owner);
            var seen = new HashSet<string>();
            for (uint seed = 0; seed < 48; seed++)
            {
                var options = Generate(owner, seed);
                Check(options.Count == 3 && options.Select(option => option.TextKey).Distinct().Count() == 3,
                    "Native maximum of three distinct options");
                foreach (var option in options)
                {
                    var pool = _unlockedPools.Single(candidate =>
                        option.TextKey == OptionPrefix + candidate.EnergyColorName.ToUpperInvariant());
                    Check(pool != owner.Character.CardPool, "No choice for the player's own pool");
                    option.Chosen().GetAwaiter().GetResult();
                    Check(_reward == (owner, pool), "Native option callback receives the owner and selected pool");
                    seen.Add(option.TextKey);
                }
            }
            Check(nightfarers.All(character => seen.Contains(OptionPrefix + character.CardPool.EnergyColorName.ToUpperInvariant())),
                "Every Nightfarer can appear");

            var teammate = PlayerFor(nightfarers[0]);
            RunStateFor(owner, teammate);
            _unlockedPools = new[] { owner.Character.CardPool, teammate.Character.CardPool, nightfarers[1].CardPool };
            foreach (var player in new[] { owner, teammate })
            {
                var options = Generate(player, 7);
                Check(options.Select(option => option.TextKey).SequenceEqual(Generate(player, 7).Select(option => option.TextKey)),
                    "Multiplayer peers regenerate the same ordered options for each owner");
                Check(options.Count == 2 && options.All(option =>
                    option.TextKey != OptionPrefix + player.Character.CardPool.EnergyColorName.ToUpperInvariant()),
                    "Multiplayer keeps the native owner's unlocked pool filter");
            }
            Console.WriteLine("PASS: Colorful Philosophers uses all five Nightfarer pools, four locales, native reward callbacks and deterministic per-player choices.");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
        }

        void Patch(System.Reflection.MethodInfo original, string prefix) => harmony.Patch(original,
            prefix: new HarmonyMethod(typeof(ColorfulPhilosophersRegression), prefix));
    }

    private static IReadOnlyList<EventOption> Generate(Player owner, uint seed)
    {
        var model = ModelDb.GetById<ColorfulPhilosophers>(ModelDb.GetId(typeof(ColorfulPhilosophers)));
        var instance = (ColorfulPhilosophers)model.MutableClone();
        AccessTools.Field(typeof(EventModel), "<Owner>k__BackingField").SetValue(instance, owner);
        AccessTools.Field(typeof(EventModel), "<Rng>k__BackingField").SetValue(instance, new Rng(seed));
        return (IReadOnlyList<EventOption>)AccessTools.Method(typeof(ColorfulPhilosophers), "GenerateInitialOptions")
            .Invoke(instance, null)!;
    }

    private static Player PlayerFor(CharacterModel character)
    {
        var player = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(player, character);
        AccessTools.Field(typeof(Player), "<UnlockState>k__BackingField").SetValue(player, UnlockState.none);
        return player;
    }

    private static void RunStateFor(params Player[] players)
    {
        var run = (RunState)RuntimeHelpers.GetUninitializedObject(typeof(RunState));
        AccessTools.Field(typeof(RunState), "_players").SetValue(run, players.ToList());
        foreach (var player in players)
            AccessTools.Field(typeof(Player), "_runState").SetValue(player, run);
    }

    private static bool Unlocks(ref IEnumerable<CardPoolModel> __result)
    {
        __result = _unlockedPools;
        return false;
    }

    private static bool EmptyEncounterCatalog(ref IEnumerable<EncounterModel> __result)
    {
        __result = Array.Empty<EncounterModel>();
        return false;
    }

    private static bool Exists(string table, string key, ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool Format(LocString __instance, ref string __result)
    {
        __result = __instance.LocEntryKey;
        return false;
    }

    private static bool RecordRewards(ColorfulPhilosophers __instance, CardPoolModel pool, ref Task __result)
    {
        _reward = (__instance.Owner, pool);
        __result = Task.CompletedTask;
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
