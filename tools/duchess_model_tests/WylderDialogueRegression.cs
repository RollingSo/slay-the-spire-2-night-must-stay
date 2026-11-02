using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Patches;

internal static class WylderDialogueRegression
{
    private static Dictionary<string, string> _table = new();

    public static void Run()
    {
        var fixture = new Harmony("NightMustStay.Wylder.AncientDialogue.Regression");
        fixture.Patch(AccessTools.Method(typeof(LocString), nameof(LocString.Exists), new[] { typeof(string), typeof(string) }),
            prefix: new HarmonyMethod(typeof(WylderDialogueRegression), nameof(Exists)));
        fixture.Patch(AccessTools.Method(typeof(LocString), nameof(LocString.GetRawText)),
            prefix: new HarmonyMethod(typeof(WylderDialogueRegression), nameof(Raw)));
        int checks = 0;
        try
        {
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                _table = new();
                foreach (string path in new[] { $"D:/STS2/localization/{locale}/ancients.json", $"NightMustStay/localization/{locale}/ancients.json" })
                {
                    using var json = JsonDocument.Parse(File.ReadAllText(path));
                    foreach (var entry in json.RootElement.EnumerateObject())
                        _table[entry.Name] = entry.Value.GetString()!;
                }
                foreach (Type type in new[] { typeof(Neow), typeof(Darv), typeof(Nonupeipe), typeof(Orobas),
                    typeof(Pael), typeof(Tanx), typeof(Tezcatara), typeof(Vakuu) })
                {
                    if (!ModelDb.Contains(type))
                        typeof(ModelDb).GetMethod("Inject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, new object[] { type });
                    var ancient = ModelDb.GetById<AncientEventModel>(ModelDb.GetId(type));
                    var set = NewSet();
                    // Call the actual runtime postfix, not a hand-written copy
                    // of the dialogue lengths, speaker rules or repeating logic.
                    GuardianAncientDialoguePatch.AddGuardianDialogue(ancient, set);
                    Check(set, ancient.Id.Entry, locale, ref checks);
                    if (type == typeof(Neow))
                    {
                        if (set.CharacterDialogues["WYLDER"][0].Lines[0].Speaker != AncientDialogueSpeaker.Ancient)
                            throw new Exception("Neow's first Wylder line must belong to Neow, not the character fallback.");
                        // Prove the fixture rejects the exact startup failure,
                        // including missing Next, rather than accepting raw keys.
                        foreach (string key in new[] { "NEOW.talk.WYLDER.0-0.ancient", "NEOW.talk.WYLDER.0-0.next" })
                        {
                            string value = _table[key];
                            _table.Remove(key);
                            bool caught = false;
                            try { set.PopulateLocKeys("NEOW"); Read(set.CharacterDialogues["WYLDER"][0]); }
                            catch (InvalidOperationException) { caught = true; }
                            finally { _table[key] = value; set.PopulateLocKeys("NEOW"); }
                            if (!caught) throw new Exception("Missing Ancient key was silently accepted: " + key);
                        }
                    }
                    // The native PopulateLocKeys mutates lines. Roles must not
                    // share dialogue instances or later keys overwrite Wylder's.
                    if (ReferenceEquals(set.CharacterDialogues["WYLDER"][0], set.CharacterDialogues["GUARDIAN"][0]))
                        throw new Exception("Characters share mutable Ancient dialogues.");
                }
                var architect = NewSet();
                AccessTools.Method(typeof(GuardianArchitectDialoguePatch), "EnsureCharacterDialogues")!
                    .Invoke(null, new object[] { architect, "WYLDER" });
                Check(architect, "THE_ARCHITECT", locale, ref checks);
            }
            Console.WriteLine($"PASS: {checks} Wylder Ancient dialogue lines in four locales, native speaker/repeating/Next resolution, visits 0-12, and missing-key regression probes.");
        }
        finally { fixture.UnpatchAll(fixture.Id); _table.Clear(); }
    }

    private static AncientDialogueSet NewSet() => new()
    {
        FirstVisitEverDialogue = null,
        CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>(),
        AgnosticDialogues = Array.Empty<AncientDialogue>(),
    };

    private static void Check(AncientDialogueSet set, string ancient, string locale, ref int checks)
    {
        foreach (var dialogue in set.CharacterDialogues["WYLDER"])
        {
            Read(dialogue);
            foreach (var line in dialogue.Lines)
            {
                if (!line.LineText!.LocEntryKey.StartsWith(ancient + ".talk.WYLDER."))
                    throw new Exception($"{locale}: Wrong character/ancient key: {line.LineText.LocEntryKey}");
                checks++;
            }
        }
        for (int visit = 0; visit <= 12; visit++)
            if (!set.GetValidDialogues(ModelDb.Character<Wylder>().Id, visit, Math.Max(1, visit), false).Any())
                throw new Exception($"{locale}/{ancient}: Empty dialogue pool at visit {visit}");
    }

    private static void Read(AncientDialogue dialogue)
    {
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            var line = dialogue.Lines[i];
            _ = line.LineText!.GetRawText();
            if (i < dialogue.Lines.Count - 1)
            {
                if (line.NextButtonText == null) throw new InvalidOperationException("Missing Next LocString");
                _ = line.NextButtonText.GetRawText();
            }
        }
    }

    public static bool Exists(string table, string key, ref bool __result)
    {
        if (table != "ancients") return true;
        __result = _table.ContainsKey(key);
        return false;
    }

    public static bool Raw(LocString __instance, ref string __result)
    {
        if (__instance.LocTable != "ancients") return true;
        if (!_table.TryGetValue(__instance.LocEntryKey, out string? text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Missing Ancient dialogue: " + __instance.LocEntryKey);
        __result = text;
        return false;
    }
}
