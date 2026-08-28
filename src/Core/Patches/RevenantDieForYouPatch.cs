using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(DieForYouPower), nameof(DieForYouPower.ModifyUnblockedDamageTarget))]
public static class RevenantDieForYouPatch
{
    [HarmonyPrefix]
    public static bool UseOrderedRevenantDamageRouting(
        DieForYouPower __instance,
        Creature target,
        ref Creature __result)
    {
        Creature revenant = __instance.Owner.PetOwner?.Creature;
        if (revenant == null ||
            !revenant.Powers.OfType<RevenantSummonControllerPower>().Any())
        {
            return true;
        }

        // Family and Necro keep the visible stock power, but the Revenant's
        // controller owns the actual family -> Necro -> Revenant routing. If
        // the stock hook also redirects, both summons can consume the same
        // damage instance or a routed hit can recurse.
        __result = target;
        return false;
    }
}
