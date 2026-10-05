using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;

// A self-shuffling reaction card can be drawn by Pocketwatch before its
// previous play finishes. Native cleanup must consume the OLD discount, not
// the reaction discount earned by drawing it again during that play.
[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AfterCardPlayedCleanup))]
public static class DuchessReactionCostCleanupPatch
{
    [HarmonyPostfix]
    public static void RestoreNewDrawDiscount(CardModel ____card, ref bool __result)
    {
        if (____card is DuchessCard card && card.RestoreReactionDiscountAfterPlayCleanup())
            __result = true;
    }
}
