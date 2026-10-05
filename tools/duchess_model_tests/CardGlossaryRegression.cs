using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Patches;

internal static class CardGlossaryRegression
{
    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new();

    public static void Run()
    {
        var fixture = new HarmonyLib.Harmony("NightMustStay.CardGlossary.Regression");
        void Patch(MethodBase target, string prefix) => fixture.Patch(target,
            prefix: new HarmonyMethod(typeof(CardGlossaryRegression).GetMethod(prefix, BindingFlags.Static | BindingFlags.Public)!));
        Patch(AccessTools.Method(typeof(LocString), nameof(LocString.GetRawText)), nameof(Raw));
        Patch(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText)), nameof(Formatted));
        Patch(AccessTools.Method(typeof(LocString), nameof(LocString.Exists), new[] { typeof(string), typeof(string) }), nameof(Exists));
        // Only asset-bearing factories are substituted; real card extras, glossary routing,
        // localization identities, keyword tips and generated-card models still run.
        Patch(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.FromPower), new[] { typeof(PowerModel), typeof(int?) }), nameof(Power));
        Patch(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.ForEnergy), new[] { typeof(CardModel) }), nameof(Energy));
        Patch(AccessTools.PropertyGetter(typeof(EnchantmentModel), nameof(EnchantmentModel.HoverTips)), nameof(Enchantment));
        fixture.CreateClassProcessor(typeof(GuardianCardHoverTipPatch)).Patch();
        int checks = 0;
        try
        {
            var pools = new CardPoolModel[] { ModelDb.CardPool<GuardianCardPool>(), ModelDb.CardPool<IroneyeCardPool>(),
                ModelDb.CardPool<RevenantCardPool>(), ModelDb.CardPool<DuchessCardPool>() };
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                Load(locale);
                foreach (CardModel canonical in pools.SelectMany(pool => pool.AllCards).Distinct())
                foreach (bool upgraded in new[] { false, true })
                {
                    CardModel card = canonical.ToMutable();
                    if (upgraded) card.UpgradeInternal();
                    try
                    {
                        IHoverTip[] tips = card.HoverTips.ToArray();
                        var extraGetter = typeof(CardModel).GetProperty("ExtraHoverTips", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        IHoverTip[] originalExtras = ((IEnumerable<IHoverTip>)extraGetter.GetValue(card)!).ToArray();
                        foreach (IHoverTip preview in originalExtras.Where(tip => tip is CardHoverTip || tip.CanonicalModel is PowerModel power
                            && new[] { typeof(HelenStepStrikePower), typeof(HelenRetreatPower), typeof(FrederickHeavyHammerPower),
                                typeof(FrederickHeadbuttPower), typeof(SebastianRoarPower), typeof(SebastianSlamPower) }.Contains(power.GetType())))
                            if (!tips.Any(tip => tip.Id == preview.Id))
                                throw new Exception("Generated-card and Family-action previews must remain intact.");
                        foreach (var keyword in card.Keywords)
                            if (!tips.Any(tip => tip.Id == HoverTipFactory.FromKeyword(keyword).Id))
                                throw new Exception("Native keyword explanations must remain intact.");
                        var glossaryType = typeof(DuchessCard).Assembly.GetType("NightMustStay.Core.Models.Cards.CardGlossary")!;
                        var mechanicTypes = (Type[])glossaryType.GetField("Mechanics", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                        string text = (string)typeof(GuardianCardHoverTipPatch).GetMethod("GetAllDescriptionText", BindingFlags.Static | BindingFlags.NonPublic)!
                            .Invoke(null, new object[] { card })!;
                        foreach (Type type in mechanicTypes)
                        {
                            string title = ModelDb.GetById<PowerModel>(ModelDb.GetId(type)).Title.GetFormattedText();
                            if (text.Contains("[gold]" + title + "[/gold]", StringComparison.OrdinalIgnoreCase)
                                && !tips.OfType<HoverTip>().Any(tip => tip.Title == title && tip.Icon == null && tip.CanonicalModel == null))
                                throw new Exception($"Missing iconless glossary for {title}.");
                        }
                        if (tips.Any(tip => tip.CanonicalModel is PowerModel p && p.Id.Entry == card.Id.Entry + "_POWER"))
                            throw new Exception("A card must not preview its own unapplied power.");
                        if (tips.Select(tip => tip.Id).Distinct().Count() != tips.Length)
                            throw new Exception("Glossary tips must be deduplicated.");
                        foreach (HoverTip tip in tips.OfType<HoverTip>().Where(tip => tip.Id.Contains("NMS_GLOSSARY_")))
                            if (tip.Icon != null || tip.Description.Contains('0') || tip.Description.Contains('{'))
                                throw new Exception("Glossary must be iconless and have no zero-layer or unresolved values.");
                        if (card is DuchessPhantomKiller && !tips.OfType<HoverTip>().Any(tip =>
                            tip.Title == ModelDb.Power<DuchessConcealmentPower>().Title.GetFormattedText() && tip.Icon == null))
                            throw new Exception("Phantom Killer must explain Concealment.");
                        if (card is DuchessRadiantBladeMagic && (!tips.OfType<CardHoverTip>().Any(tip => tip.Card is DuchessRadiantBlade)
                            || tips.Any(tip => tip.CanonicalModel is DuchessRadiantBladeGrowthPower)))
                            throw new Exception("Radiant Blade Magic must preview Radiant Blade, not a zero-growth power.");
                        checks++;
                    }
                    catch (Exception error) { throw new Exception($"{locale}/{card.Id.Entry}, upgraded={upgraded}: {error.Message}", error); }
                }
            }
            Console.WriteLine($"PASS: {checks} card hover-tip checks across four characters, four locales and both upgrade states (asset factories stubbed).");
        }
        finally { fixture.UnpatchAll(fixture.Id); }
    }

    private static void Load(string locale)
    {
        Tables.Clear();
        foreach (string root in new[] { $"D:/STS2/localization/{locale}", $"NightMustStay/localization/{locale}" })
        foreach (string path in Directory.GetFiles(root, "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!Tables.TryGetValue(name, out var table)) Tables[name] = table = new();
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (JsonProperty entry in document.RootElement.EnumerateObject())
                if (entry.Value.ValueKind == JsonValueKind.String) table[entry.Name] = entry.Value.GetString()!;
        }
    }

    public static bool Exists(string table, string key, ref bool __result)
    { __result = Tables.TryGetValue(table, out var values) && values.ContainsKey(key); return false; }
    public static bool Raw(LocString __instance, ref string __result)
    {
        __result = Tables.TryGetValue(__instance.LocTable, out var table) && table.TryGetValue(__instance.LocEntryKey, out string? value)
            ? value : __instance.LocEntryKey;
        return false;
    }
    public static bool Formatted(LocString __instance, ref string __result)
    {
        Raw(__instance, ref __result);
        __result = Regex.Replace(__result, @"\{([A-Za-z0-9_]+)(?::[^{}]*)?\}", match =>
            __instance.Variables.TryGetValue(match.Groups[1].Value, out object? value)
                ? value is DynamicVar variable ? variable.BaseValue.ToString() : value.ToString()! : match.Value);
        return false;
    }
    public static bool Power(PowerModel model, ref IHoverTip __result) { __result = new AssetFreeTip(model); return false; }
    public static bool Energy(ref IHoverTip __result) { __result = new AssetFreeTip(null); return false; }
    public static bool Enchantment(EnchantmentModel __instance, ref IEnumerable<IHoverTip> __result)
    { __result = new[] { new AssetFreeTip(__instance) }; return false; }
    private sealed record AssetFreeTip(AbstractModel? CanonicalModel) : IHoverTip
    {
        public string Id => CanonicalModel?.Id.ToString() ?? "fixture.energy";
        public bool IsSmart => false;
        public bool IsDebuff => false;
        public bool IsInstanced => false;
    }
}
