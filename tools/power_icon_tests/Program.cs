using System.Reflection;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Patches;

try
{
    BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
    typeof(ModManager).GetMethod("ResetForTests", flags)!.Invoke(null, null);
    PropertyInfo modState = typeof(ModManager).GetProperty("State")!;
    modState.SetValue(null, Enum.Parse(modState.PropertyType, "Skipped"));
    typeof(ModelDb).GetMethod("Init", flags)!.Invoke(null, null);

    Type[] models = typeof(AirRendingArrow).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(AbstractModel).IsAssignableFrom(type))
        .ToArray();
    foreach (Type type in models)
    {
        if (!ModelDb.Contains(type))
            typeof(ModelDb).GetMethod("Inject", flags)!.Invoke(null, new object[] { type });
    }

    PropertyInfo bigIconPath = typeof(PowerModel).GetProperty(
        "BigIconPath",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    string root = Directory.GetCurrentDirectory();
    var cases = new (string Name, PowerModel Power, string IconPath, string BigIconPath, string RootPath)[]
    {
        (
            "Air Rending Arrow",
            ModelDb.Power<AirRendingArrowStrengthDownPower>(),
            "res://images/atlases/power_atlas.sprites/air_rending_arrow_strength_down_power.tres",
            "res://images/powers/air_rending_arrow_strength_down_power.png",
            "res://powers/air_rending_arrow_strength_down_power.png"),
        (
            "Mark",
            ModelDb.Power<NightMustStayMarkPower>(),
            "res://images/atlases/power_atlas.sprites/night_must_stay_mark_power.tres",
            "res://images/powers/night_must_stay_mark_power.png",
            "res://powers/night_must_stay_mark_power.png"),
    };

    foreach (var iconCase in cases)
    {
        Console.WriteLine($"{iconCase.Name}: ID={iconCase.Power.Id}");
        Console.WriteLine($"{iconCase.Name}: IconPath={iconCase.Power.IconPath}");
        Console.WriteLine($"{iconCase.Name}: PackedIconPath={iconCase.Power.PackedIconPath}");
        Console.WriteLine($"{iconCase.Name}: BigIconPath={bigIconPath.GetValue(iconCase.Power)}");
        if (iconCase.Power.IconPath != iconCase.IconPath
            || bigIconPath.GetValue(iconCase.Power) as string != iconCase.BigIconPath)
        {
            throw new InvalidOperationException(
                $"{iconCase.Name} power model resolves unexpected icon paths.");
        }

        string resolved = string.Empty;
        bool runOriginal = IroneyeAssetPatch.ResolveIroneyePowerBigIcon(
            iconCase.Power,
            ref resolved);
        if (runOriginal || resolved != iconCase.BigIconPath)
        {
            throw new InvalidOperationException(
                $"{iconCase.Name} big-icon fallback was not bypassed.");
        }

        foreach (string path in new[]
        {
            iconCase.IconPath,
            iconCase.BigIconPath,
            iconCase.RootPath,
        })
        {
            string file = Path.Combine(root, path[6..].Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
                throw new FileNotFoundException($"Required {iconCase.Name} power icon is missing.", file);
        }
    }

    string untouched = "sentinel";
    if (!IroneyeAssetPatch.ResolveIroneyePowerBigIcon(
            ModelDb.Power<DistancePower>(),
            ref untouched)
        || untouched != "sentinel")
    {
        throw new InvalidOperationException("Unrelated power icon resolution was intercepted.");
    }

    MethodInfo getter = typeof(PowerModel).GetProperty(nameof(PowerModel.ResolvedBigIconPath))!.GetMethod!;
    MethodInfo prefix = typeof(IroneyeAssetPatch).GetMethod(
        nameof(IroneyeAssetPatch.ResolveIroneyePowerBigIcon))!;
    var harmony = new HarmonyLib.Harmony("NightMustStay.PowerIcon.Tests");
    harmony.Patch(getter, prefix: new HarmonyLib.HarmonyMethod(prefix));
    harmony.UnpatchAll(harmony.Id);
    Console.WriteLine("PASS: Ironeye power icon paths, assets, fallback overrides, and patch binding.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
