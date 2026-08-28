using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
    internal static class IroneyeDistanceAmountClampPatch
    {
        private static void Prefix(PowerModel __instance, ref int amount)
        {
            if (__instance is DistancePower)
            {
                amount = System.Math.Clamp(
                    amount,
                    DistancePower.MinimumDistance,
                    DistancePower.MaximumDistance);
            }
        }
    }

    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ShouldRemoveDueToAmount))]
    internal static class IroneyeDistancePowerPatch
    {
        private static void Postfix(PowerModel __instance, ref bool __result)
        {
            if (__instance is DistancePower
                && __instance.Owner?.Player?.Character is Ironeye)
            {
                __result = false;
            }
        }
    }
}
