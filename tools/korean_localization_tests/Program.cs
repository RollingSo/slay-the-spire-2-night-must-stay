using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SmartFormat;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var tables = Directory.GetFiles(Path.Combine(root, "NightMustStay/localization/kor"), "*.json")
    .ToDictionary(path => Path.GetFileNameWithoutExtension(path)!,
        path => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!)!;

// Use the installed game's real formatter without initializing Godot or saves.
var manager = (LocManager)RuntimeHelpers.GetUninitializedObject(typeof(LocManager));
const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
var locTables = tables.ToDictionary(pair => pair.Key!, pair => new LocTable(pair.Key!, pair.Value, null));
typeof(LocManager).GetField("_tables", flags)!.SetValue(manager, locTables);
typeof(LocManager).GetField("_engTables", flags)!.SetValue(manager, locTables);
typeof(LocManager).GetProperty("Instance", flags)!.SetValue(null, manager);
typeof(LocManager).GetProperty("CultureInfo", flags)!.SetValue(manager, CultureInfo.GetCultureInfo("ko-KR"));
typeof(LocManager).GetProperty("Language", flags)!.SetValue(manager, "kor");
typeof(LocManager).GetMethod("LoadLocFormatters", flags)!.Invoke(manager, null);
var formatterField = typeof(LocManager).GetField("_smartFormatter", flags)!;
var formatter = (SmartFormatter)formatterField.GetValue(formatterField.IsStatic ? null : manager)!;

int count = 0;
foreach (var (tableName, table) in tables)
foreach (var (key, text) in table)
foreach (UpgradeDisplay display in Enum.GetValues<UpgradeDisplay>())
foreach (bool inCombat in new[] { false, true })
{
    bool upgraded = display != UpgradeDisplay.Normal;
    var loc = new LocString(tableName!, key);
    foreach (string token in Regex.Matches(text, @"\{([A-Za-z][A-Za-z0-9_]*)")
        .Select(match => match.Groups[1].Value).Distinct())
    {
        if (token is "IfUpgraded")
            loc.Add(new IfUpgradedVar(display));
        else if (token is "InCombat" or "IsTargeting")
            loc.Add(token, inCombat);
        else if (token is "ChargeStateText" or "GeneratedCard")
            loc.Add(token, "검증용 카드");
        else if (token is "energyPrefix")
            loc.Add(token, "guardian");
        else if (text.Contains("{" + token + ":energyIcons("))
        {
            var energy = new EnergyVar(token, upgraded ? 2 : 1);
            typeof(EnergyVar).GetField("<ColorPrefix>k__BackingField", flags)!.SetValue(energy, "guardian");
            loc.Add(energy);
        }
        else if (text.Contains("{" + token + ":diff("))
            loc.Add(new DynamicVar(token, upgraded ? 7m : 3m));
        else
            loc.Add(token, upgraded ? 7m : 3m);
    }
    try
    {
        string rendered = formatter.Format(CultureInfo.GetCultureInfo("ko-KR"), text, loc.Variables);
        if (Regex.IsMatch(rendered, @"\{[A-Za-z]") || rendered.Contains("LOC ERROR", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unresolved localization: " + rendered);
        string plain = Regex.Replace(rendered, @"\[img[^\]]*\].*?\[/img\]", "", RegexOptions.Singleline);
        plain = Regex.Replace(plain, @"\[[^\]]+\]", "");
        if (Regex.IsMatch(plain, @"[\u3400-\u9fff\u3040-\u30ff]") ||
            Regex.Matches(plain, @"[A-Za-z]+").Any(m => m.Value != "X"))
            throw new InvalidOperationException("Untranslated visible text: " + rendered);
        count++;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{tableName}/{key} ({display}, combat={inCombat}): {text}\n{ex}");
        return 1;
    }
}
Console.WriteLine($"PASS: {tables.Sum(t => t.Value.Count)} Korean entries, {count} native formatter cases.");
return 0;
