using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;
[HarmonyPatch]
internal static class WylderPursuitDamageBranchPatch
{
    private static MethodBase TargetMethod() => DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");
    [HarmonyPrefix]
    private static bool Modify(AbstractModel __instance, Creature dealer, ValueProp props,
        CardModel cardSource, ref decimal __result)
    {
        if (__instance is not WylderPursuitPower power) return true;
        __result = power.AttackMultiplier(dealer, props, cardSource);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TargetType), MethodType.Getter)]
internal static class WylderSweepTargetPatch
{
    [HarmonyPostfix]
    private static void Target(CardModel __instance, ref TargetType __result)
    {
        if (__instance is WylderStampSweep)
            __result = WylderPursuitPower.IsActive(__instance.Owner?.Creature)
                ? TargetType.AllEnemies : TargetType.AnyEnemy;
    }
}
