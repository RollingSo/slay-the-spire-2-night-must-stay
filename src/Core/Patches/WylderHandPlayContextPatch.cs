using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class WylderCaptureHandSourcePatch
{
    [HarmonyPrefix]
    private static void Capture(CardModel __instance) => WylderHandPlayContext.CaptureWrapper(__instance);
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
internal static class WylderRecordHandSourcePatch
{
    [HarmonyPrefix]
    private static void Record(CardPlay cardPlay)
    {
        WylderHandPlayContext.Record(cardPlay);
        WylderCombatEvents.BeginCard(cardPlay);
    }
}
