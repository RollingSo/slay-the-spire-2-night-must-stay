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
    MethodInfo init = typeof(ModelDb).GetMethod("Init", flags)!;
    init.Invoke(null, init.GetParameters().Length == 0 ? null : new object[] { Type.EmptyTypes });

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
    var cases = new (string Name, PowerModel Power, string IconPath, string BigIconPath)[]
    {
        (
            "Air Rending Arrow",
            ModelDb.Power<AirRendingArrowStrengthDownPower>(),
            "res://images/atlases/power_atlas.sprites/air_rending_arrow_strength_down_power.tres",
            "res://ironeye_assets/powers/air_rending_arrow_strength_down_power.png"),
        (
            "Mark",
            ModelDb.Power<NightMustStayMarkPower>(),
            "res://images/atlases/power_atlas.sprites/night_must_stay_mark_power.tres",
            "res://ironeye_assets/powers/night_must_stay_mark_power.png"),
    };

    foreach (var iconCase in cases)
    {
        Console.WriteLine($"{iconCase.Name}: ID={iconCase.Power.Id}");
        Console.WriteLine($"{iconCase.Name}: IconPath={iconCase.Power.IconPath}");
        Console.WriteLine($"{iconCase.Name}: PackedIconPath={iconCase.Power.PackedIconPath}");
        Console.WriteLine($"{iconCase.Name}: BigIconPath={bigIconPath.GetValue(iconCase.Power)}");
        if (iconCase.Power.IconPath != iconCase.IconPath)
        {
            throw new InvalidOperationException(
                $"{iconCase.Name} power model resolves an unexpected compact icon path.");
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

        MethodInfo iconPrefix = typeof(IroneyeAssetPatch).GetMethod(
            nameof(IroneyeAssetPatch.ResolveIroneyePowerIcon))!;
        MethodInfo bigIconPrefix = typeof(IroneyeAssetPatch).GetMethod(
            nameof(IroneyeAssetPatch.ResolveIroneyePowerBigIconTexture))!;
        if (iconPrefix.GetCustomAttributes(typeof(HarmonyLib.HarmonyPrefix), false).Length == 0
            || bigIconPrefix.GetCustomAttributes(typeof(HarmonyLib.HarmonyPrefix), false).Length == 0)
        {
            throw new InvalidOperationException(
                $"{iconCase.Name} icon texture getters are not protected by Harmony prefixes.");
        }

        foreach (string path in new[]
        {
            iconCase.IconPath,
            iconCase.BigIconPath,
        })
        {
            string file = Path.Combine(root, path[6..].Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
                throw new FileNotFoundException($"Required {iconCase.Name} power icon is missing.", file);
        }
    }

    PropertyInfo markDisplayAmount = typeof(NightMustStayMarkPower).GetProperty(
        nameof(PowerModel.DisplayAmount))!;
    if (markDisplayAmount.GetMethod?.DeclaringType != typeof(NightMustStayMarkPower))
    {
        throw new InvalidOperationException(
            "Mark must explicitly bind its displayed stack count to Amount.");
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
    Console.WriteLine("PASS: Ironeye power icon paths, assets, path/texture fallback overrides, and patch binding.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
