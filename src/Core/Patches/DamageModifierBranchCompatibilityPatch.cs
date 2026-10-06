using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch]
internal static class StyxSpiritFireDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageAdditive");

    [HarmonyPrefix]
    private static bool BeforeModify(AbstractModel __instance, Creature target,
        CardModel cardSource, ref decimal __result)
    {
        if (__instance is not NightMustStay.Core.Models.Cards.StyxSpiritFire card || cardSource != card)
            return true;
        __result = card.GetTargetFreezeDamage(target) - 1m;
        return false;
    }
}

/// <summary>
/// The Public Beta added a CardPlay parameter to PowerModel's damage modifier
/// hooks in v0.108. Resolving the current method by name keeps one mod DLL
/// compatible with both the five-parameter release API and six-parameter Beta
/// API without weakening the power effects.
/// </summary>
internal static class DamageModifierBranchCompatibility
{
    public static MethodBase Resolve(string name) =>
        // These virtual hooks are inherited by PowerModel on the release branch.
        // GetDeclaredMethods therefore returns no match at runtime even though a
        // build against sts2.dll succeeds. Search the complete instance surface
        // so the same patch resolves both the release and Public Beta signatures.
        typeof(PowerModel)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(method => method.Name == name);
}

[HarmonyPatch]
internal static class FearlessDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");

    [HarmonyPrefix]
    private static bool BeforeModify(AbstractModel __instance, Creature dealer,
        ValueProp props, CardModel cardSource, ref decimal __result)
    {
        if (__instance is not NightMustStay.Core.Models.Cards.ShieldPoke card || cardSource != card)
            return true;
        __result = props.IsPoweredAttack()
            && NightMustStay.Core.Models.Cards.Fearless.IsShieldPokeEmpowered(card) ? 2m : 1m;
        return false;
    }
}

[HarmonyPatch]
internal static class DuchessConcealmentDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");

    [HarmonyPrefix]
    private static bool BeforeModify(
        AbstractModel __instance, Creature dealer, ValueProp props, ref decimal __result)
    {
        if (__instance is not DuchessConcealmentPower power) return true;
        __result = power.GetAttackDamageMultiplier(dealer, props);
        return false;
    }
}

[HarmonyPatch]
internal static class DuchessZeroCostAttackDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageAdditive");

    [HarmonyPrefix]
    private static bool BeforeModify(
        AbstractModel __instance, Creature dealer, ValueProp props,
        CardModel cardSource, ref decimal __result)
    {
        if (__instance is not DuchessZeroCostAttackPower power) return true;
        __result = power.GetZeroCostAttackBonus(props, dealer, cardSource);
        return false;
    }
}

[HarmonyPatch]
internal static class IncomingDamageReductionBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");

    [HarmonyPrefix]
    private static bool BeforeModify(
        AbstractModel __instance,
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
internal static class SaviorSpreadWingsDamageBranchPatch
{
    private static MethodBase TargetMethod() =>
        DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");

    [HarmonyPrefix]
    private static bool BeforeModify(
        AbstractModel __instance, Creature dealer, ValueProp props, ref decimal __result)
    {
        if (__instance is not SaviorSpreadWingsPower power)
            return true;

        __result = power.GetAttackMultiplier(dealer, props);
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
        AbstractModel __instance,
        Creature target,
        decimal amount,
        ValueProp props,
        ref decimal __result)
    {
        if (__instance is not FreezePower power)
            return true;

        __result = target == power.Owner
            && amount > 0m
            && power.Owner.IsAlive
            && props.HasFlag(ValueProp.Move)
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
        AbstractModel __instance,
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
