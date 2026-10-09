using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Patches;

// Family members use Osty's entity, but not Osty's NextMove.Intents.
[HarmonyPatch(typeof(NCreature), nameof(NCreature.UpdateIntent))]
internal static class RevenantFamilyIntentRefreshPatch
{
    [HarmonyPrefix]
    private static bool BeforeUpdate(NCreature __instance, ref Task __result)
    {
        if (!RevenantSummonManager.RefreshManagedFamilyIntent(__instance.Entity)) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.AnimHideIntent))]
internal static class RevenantFamilyIntentHidePatch
{
    [HarmonyPrefix]
    private static bool BeforeHide(NCreature __instance) =>
        !RevenantSummonManager.RefreshManagedFamilyIntent(__instance.Entity, requireScheduledAction: true);
}
