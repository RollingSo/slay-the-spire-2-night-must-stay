using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AfterCardPlayedCleanup))]
internal static class RevenantChargeCostCleanupPatch
{
    [HarmonyPostfix]
    private static void RestoreNewChargeDiscount(CardModel ____card, ref bool __result)
    {
        if (BelieversVowCloth.RestoreChargeDiscountAfterPlayCleanup(____card))
            __result = true;
    }
}
