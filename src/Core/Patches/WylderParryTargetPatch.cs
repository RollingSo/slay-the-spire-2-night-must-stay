using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;

// IsValidTarget is nonvirtual. Restrict only Parry after native side/alive rules.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
internal static class WylderParryTargetPatch
{
    [HarmonyPostfix]
    private static void Restrict(CardModel __instance, Creature target, ref bool __result)
    {
        if (__result && __instance is WylderParry)
            __result = WylderCompletionRules.IsAttacking(target);
    }
}
