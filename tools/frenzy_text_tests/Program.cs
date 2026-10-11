using System.Text.Json;
using System.Text.RegularExpressions;
using SmartFormat;

// Syntax regression test using the game's SmartFormat library, without
// initializing Godot. Substitute native diff() with plain numeric formatting:
// this tests conditional syntax, not combat modifiers or diff colors.
string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
int checks = 0;
foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
{
    using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NightMustStay", "localization", locale, "cards.json")));
    foreach (string id in new[] { "SPACE_RENDING_FRENZY", "UNBEARABLE_FRENZY" })
    foreach (string suffix in new[] { "description", "upgradeDescription" })
    foreach (bool upgraded in new[] { false, true })
    foreach (int summons in new[] { 0, 1, 2 })
    foreach (bool inCombat in new[] { false, true })
    {
        string key = $"{id}.{suffix}";
        string text = table.RootElement.GetProperty(key).GetString()!;
        string syntax = text.Replace(":diff()", "");
        int multiplier = id == "SPACE_RENDING_FRENZY" ? (upgraded ? 5 : 4) : 1;
        int damage = 4 * summons * multiplier;
        var values = new Dictionary<string, object>
        {
            ["FamilyDamage"] = 4, ["ExtraDamage"] = multiplier,
            ["Repeat"] = upgraded ? 7 : 6, ["CalculatedDamage"] = damage,
            ["InCombat"] = inCombat
        };
        string rendered = Smart.Format(syntax, values);
        if (Regex.IsMatch(rendered, "[{}]")) throw new Exception($"Unresolved placeholder: {locale}/{key}: {rendered}");
        bool hasPreview = rendered.Contains($"（{damage}）") || rendered.Contains($"({damage})");
        if (hasPreview != inCombat) throw new Exception($"Incorrect combat preview: {locale}/{key}: {rendered}");
        checks++;
    }
}
Console.WriteLine($"PASS: {checks} Frenzy conditional-format cases (4 locales, base/upgrade, library/combat, 0/1/2 summons).");
