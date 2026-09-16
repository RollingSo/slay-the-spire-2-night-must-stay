using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

/// <summary>
/// Distance is an internal stance axis. Doubling it also doubles the derived
/// Dexterity and Strength adjustment, while the Lamp should only duplicate the
/// card's ordinary power effects such as Mark.
/// </summary>
[HarmonyPatch(
    typeof(UnsettlingLamp),
    nameof(UnsettlingLamp.ModifyPowerAmountGivenMultiplicative))]
public static class IroneyeUnsettlingLampPatch
{
    [HarmonyPostfix]
    private static void KeepDistanceAtNormalMultiplier(
        PowerModel canonicalPower,
        ref decimal __result)
    {
        if (canonicalPower is DistancePower)
            __result = 1m;
    }
}
