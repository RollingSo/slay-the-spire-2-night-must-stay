using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

/// <summary>
/// The Public Beta added a CardPlay parameter to PowerModel's damage modifier
/// hooks in v0.108. Resolving the current method by name keeps one mod DLL
/// compatible with both the five-parameter release API and six-parameter Beta
/// API without weakening any of the three power effects.
/// </summary>
internal static class DamageModifierBranchCompatibility
{
    public static MethodBase Resolve(string name) =>
        AccessTools.GetDeclaredMethods(typeof(PowerModel))
            .Single(method => method.Name == name);
}

[HarmonyPatch]
internal static class IncomingDamageReductionBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");

    [HarmonyPrefix]
    private static bool BeforeModify(
        PowerModel __instance,
        Creature target,
        ref decimal __result)
    {
        if (__instance is not IncomingDamageReductionThisTurnPower power)
            return true;

        __result = target == power.Owner
            ? (100m - power.Amount) / 100m
            : 1m;
        return false;
    }
}

[HarmonyPatch]
internal static class FreezeDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageAdditive");

    [HarmonyPrefix]
    private static bool BeforeModify(
        PowerModel __instance,
        Creature target,
        decimal amount,
        ref decimal __result)
    {
        if (__instance is not FreezePower power)
            return true;

        __result = target == power.Owner && amount > 0m && power.Owner.IsAlive
            ? power.Amount
            : 0m;
        return false;
    }
}

[HarmonyPatch]
internal static class WhiteShadowDamageCapBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageCap");

    [HarmonyPrefix]
    private static bool BeforeModify(
        PowerModel __instance,
        Creature target,
        ref decimal __result)
    {
        if (__instance is not WhiteShadowLurePower power)
            return true;

        __result = target == power.Owner && power.Amount > 0m
            ? 0m
            : decimal.MaxValue;
        return false;
    }
}
