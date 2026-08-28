using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;

// These upgrades change behavior without changing a DynamicVar or keyword.
// The base game can therefore retain the base description LocString for
// upgraded instances and previews. Force the upgraded localization key at the
// CardModel getter so the displayed text matches the actual extra effect.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class RevenantCallDescriptionPatch
{
    [HarmonyPostfix]
    private static void UseUpgradeDescription(CardModel __instance, ref LocString __result)
    {
        if (__instance is RevenantCall { IsUpgraded: true })
            __result = new LocString("cards", "REVENANT_CALL.upgradeDescription");
        else if (__instance is RevenantResonance { IsUpgraded: true })
            __result = new LocString("cards", "REVENANT_RESONANCE.upgradeDescription");
    }
}
