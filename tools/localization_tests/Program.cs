using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartFormat;
using SmartFormat.Core.Parsing;
using SmartFormat.Core.Settings;

// Use the parser shipped with the installed game, not a new NuGet version.
// This checks syntax and plural branches, not Godot rendering or combat logic.
string? rootArgument = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
string root = rootArgument != null ? Path.GetFullPath(rootArgument) : Directory.GetCurrentDirectory();
string locRoot = Path.Combine(root, "NightMustStay", "localization");
var errors = new List<string>();
int dynamicPreviewTests = 0;
var tables = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
var parser = new Parser(new SmartSettings());
int count = 0, pluralTests = 0;
foreach (string locale in new[] { "eng", "jpn", "kor", "zhs" })
{
    tables[locale] = new();
    foreach (string file in Directory.GetFiles(Path.Combine(locRoot, locale), "*.json"))
    {
        string name = Path.GetFileNameWithoutExtension(file);
        string json = File.ReadAllText(file, new UTF8Encoding(false, true));
        using var doc = JsonDocument.Parse(json);
        var entries = new Dictionary<string, string>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            string key = property.Name;
            string value = property.Value.GetString() ?? throw new Exception($"Null string: {file}/{key}");
            string label = $"{locale}/{name}/{key}";
            if (!entries.TryAdd(key, value)) errors.Add($"Duplicate key: {label}");
            if (value.Contains('\uFFFD')) errors.Add($"Replacement character: {label}");
            try { parser.ParseFormat(value); }
            catch (Exception e) { errors.Add($"SmartFormat parse failed: {label}: {e.Message}"); }
            var stack = new Stack<string>();
            foreach (Match m in Regex.Matches(value, @"\[(/?)(gold|blue|purple|green|sine|thinky_dots|b|i|font_size|jitter)(?:=[^\]]+)?\]"))
            {
                string tag = m.Groups[2].Value;
                if (m.Groups[1].Value == "") stack.Push(tag);
                else if (!stack.TryPop(out string? open) || open != tag) errors.Add($"Mismatched rich-text nesting: {label}");
            }
            if (stack.Count != 0) errors.Add($"Unclosed rich-text tag: {label}");
            if (locale == "eng")
            {
                foreach (Match m in Regex.Matches(value, @"\{(\w+):plural:([^{}|]+)\|([^{}|]+)\}"))
                {
                    foreach (int n in new[] { 0, 1, 2, 5 })
                    {
                        var data = new Dictionary<string, object> { [m.Groups[1].Value] = n };
                        string rendered = Smart.Format(CultureInfo.GetCultureInfo("en"), m.Value, data);
                        string expected = m.Groups[n == 1 ? 2 : 3].Value;
                        if (rendered != expected) errors.Add($"Plural mismatch ({n}): {label}");
                        pluralTests++;
                    }
                }
                if (Regex.IsMatch(value, @"\b(?:time|turn|card)\(s\)")) errors.Add($"Unresolved plural: {label}");
            }
            count++;
        }
        tables[locale][name] = entries;
    }
}
void Require(bool ok, string message) { if (!ok) errors.Add(message); }
// Native BodySlam uses the default SmartFormat boolean conditional supplied by
// CardModel.GetDescriptionForPile. Exercise both branches in all four locales.
int errorsBeforeDynamicPreviews = errors.Count;
foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
foreach (string cardId in new[] { "SHIELD_IMPACT", "BLADE_GLIDE", "WORLD_ENDING_WINGS",
             "DUCHESS_MIQUELLAS_HALO", "DUCHESS_SACRED_HALO", "DUCHESS_REENACTMENT", "DUCHESS_FLEETING_INSTANT" })
foreach (string suffix in new[] { "description", "upgradeDescription" })
foreach (bool upgraded in new[] { false, true })
{
    string label = $"{locale}/{cardId}.{suffix}";
    string description = tables[locale]["cards"][cardId + "." + suffix];
    Require(description.Contains("{InCombat:"), $"Missing native combat-only preview: {label}");
    string format = Regex.Replace(description, @"\{IfUpgraded:show:([^{}|]*)\|([^{}]*)\}",
        m => m.Groups[upgraded ? 1 : 2].Value).Replace(":diff()", "");
    foreach (int damage in new[] { 0, 1, 26 })
    foreach (bool inCombat in new[] { false, true })
    {
        var values = new Dictionary<string, object>();
        foreach (Match field in Regex.Matches(format, @"\{(\w+)(?:[:}])"))
            values[field.Groups[1].Value] = 7;
        values["InCombat"] = inCombat;
        values["CalculatedDamage"] = damage;
        try
        {
            string actual = Smart.Format(CultureInfo.InvariantCulture, format, values);
            values["CalculatedDamage"] = damage + 100;
            string changed = Smart.Format(CultureInfo.InvariantCulture, format, values);
            Require(inCombat ? actual != changed : actual == changed,
                $"Calculated result must affect only combat text: {label}, upgraded={upgraded}, InCombat={inCombat}");
            Require(!actual.Contains('{'), $"Unresolved preview expression: {label}");
        }
        catch (Exception e) { errors.Add($"Dynamic preview formatting failed: {label}: {e.Message}"); }
        dynamicPreviewTests++;
    }
}
// Narrow mode isolates this regression suite; the default still runs every
// existing copy-edit assertion and reports all pre-existing failures.
foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
foreach (string suffix in new[] { "description", "upgradeDescription" })
foreach (int hits in new[] { 1, 2, 4 })
foreach (bool inCombat in new[] { false, true })
{
    string format = tables[locale]["cards"]["WHIRLING_STRIKE." + suffix].Replace(":diff()", "");
    var values = new Dictionary<string, object> { ["Damage"] = suffix == "description" ? 5 : 7,
        ["CalculatedHits"] = hits, ["InCombat"] = inCombat };
    try
    {
        string actual = Smart.Format(CultureInfo.InvariantCulture, format, values);
        values["CalculatedHits"] = hits + 100;
        string changed = Smart.Format(CultureInfo.InvariantCulture, format, values);
        Require(inCombat ? actual != changed : actual == changed,
            $"Whirling Strike hit count must affect only combat text: {locale}/{suffix}, hits={hits}");
    }
    catch (Exception e) { errors.Add($"Whirling Strike preview formatting failed: {locale}/{suffix}: {e.Message}"); }
    dynamicPreviewTests++;
}
if (args.Contains("--dynamic-preview-only"))
{
    foreach (string error in errors.Skip(errorsBeforeDynamicPreviews)) Console.Error.WriteLine(error);
    if (errors.Count != errorsBeforeDynamicPreviews) return 1;
    Console.WriteLine($"PASS: {dynamicPreviewTests} native combat/library preview cases across four locales.");
    return 0;
}
foreach (string locale in new[] { "eng", "jpn", "kor" })
{
    var cards = tables[locale]["cards"];
    foreach (var (key, value) in cards)
        if (key.EndsWith(".upgradeDescription")) Require(!value.Contains('→'), $"Delta-only upgrade: {locale}/{key}");
    string poke = cards["SHIELD_POKE.title"] + "+";
    Require(tables[locale]["potions"]["FEARLESS_LIQUOR.description"].Contains(poke), $"Missing upgraded Shield Poke name: {locale}");
    Require(tables[locale]["relics"]["TACTICAL_COMPENDIUM.description"].Contains(poke), $"Missing upgraded Shield Poke in relic: {locale}");
    foreach (string id in new[] { "RESURGENCE", "SPIRIT_GATHERING", "BURN_LIFE" })
        Require(cards[id + ".upgradeDescription"].Contains("{Energy:energyIcons()}"), $"Energy icon missing: {locale}/{id}");
    foreach (string id in new[] { "REVERSAL_STEP", "STALWART_SHIELD" })
        Require(cards[id + ".upgradeDescription"] == cards[id + ".description"], $"Canonical-only upgrade duplicated in rules text: {locale}/{id}");
    string step = cards["APPROACH.title"] + "+";
    string retreat = cards["RETREAT.title"] + "+";
    string ghostStep = tables[locale]["powers"]["HEAVENLY_EYE_FORM_POWER.description"];
    Require(ghostStep.Contains(step) && ghostStep.Contains(retreat), $"Ghost Step must name upgraded cards: {locale}");
}
Require(tables["jpn"]["powers"]["DIE_FOR_YOU_POWER.title"] == "身代わり", "Native Japanese Die for You name regression");
Require(tables["kor"]["powers"]["DIE_FOR_YOU_POWER.title"] == "살신성인", "Native Korean Die for You name regression");
Require(!tables["jpn"].Values.SelectMany(t => t.Values).Any(s => s.Contains("敏捷性") || s.Contains("インク化") || s.Contains("建築家")), "Japanese terminology regression");
Require(!tables["kor"].Values.SelectMany(t => t.Values).Any(s => s.Contains("방패 콕 찌르기")), "Korean Shield Poke name regression");
if (errors.Count > 0)
{
    foreach (string error in errors) Console.Error.WriteLine(error);
    return 1;
}
Console.WriteLine($"Localization tests passed: {count} strings parsed, {pluralTests} English plural cases, {dynamicPreviewTests} native combat/library preview cases, rich-text nesting and copy-edit regressions.");
return 0;
