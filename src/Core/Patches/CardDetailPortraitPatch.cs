using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Patches;

[HarmonyPatch]
internal static class CardDetailPortraitPatch
{
    // Older game branches use Portrait in the inspect screen directly.
    private static bool Prepare() => AccessTools.PropertyGetter(typeof(CardModel), "BigPortrait") != null;

    private static MethodBase TargetMethod() =>
        AccessTools.PropertyGetter(typeof(CardModel), "BigPortrait");

    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.GetType().Assembly != typeof(CardDetailPortraitPatch).Assembly)
            return true;

        __result = __instance.Portrait;
        return false;
    }
}
