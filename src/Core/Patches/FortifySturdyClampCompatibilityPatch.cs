using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

// Adjust the relic's own retention cap. Fortify never issues a block-loss
// command, and ordinary damage/explicit block loss is unaffected.
[HarmonyPatch]
internal static class FortifySturdyClampRetentionPatch
{
    private static MethodBase TargetMethod()
    {
        var method = AccessTools.Method(typeof(SturdyClamp), nameof(SturdyClamp.AfterPreventingBlockClear));
        var state = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            ?? throw new InvalidOperationException("SturdyClamp retention is no longer async.");
        return AccessTools.Method(state, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var target = TargetMethod().DeclaringType;
        var relic = AccessTools.Field(target, "<>4__this");
        var cap = AccessTools.Method(typeof(FortifySturdyClampRetentionPatch), nameof(RetentionCap));
        int replacements = 0;
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (!instruction.LoadsConstant(10)) continue;
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, relic);
            yield return new CodeInstruction(OpCodes.Call, cap);
            replacements++;
        }
        if (replacements != 2)
            throw new InvalidOperationException("Expected SturdyClamp's cap comparison and loss calculation.");
    }

    private static int RetentionCap(int nativeCap, SturdyClamp relic) =>
        Math.Max(nativeCap, relic.Owner.Creature.GetPower<FortifyPower>()?.Amount ?? 0);
}

[HarmonyPatch(typeof(SturdyClamp), nameof(SturdyClamp.AfterPreventingBlockClear))]
internal static class FortifySturdyClampNotificationPatch
{
    private static void Postfix(SturdyClamp __instance, AbstractModel preventer, Creature creature, ref Task __result)
    {
        if (preventer == __instance && creature == __instance.Owner.Creature
            && creature.GetPower<FortifyPower>() is { } fortify)
            __result = NotifyAfterRetention(__result, creature, fortify);
    }

    private static async Task NotifyAfterRetention(Task retention, Creature creature, FortifyPower fortify)
    {
        await retention;
        fortify.NotifyAutomaticClearRetention(fortify.GetAutomaticClearRetainedBlock(creature.Block));
    }
}

[HarmonyPatch(typeof(SturdyClamp), nameof(SturdyClamp.ShouldClearBlock))]
internal static class FortifySturdyClampBarricadePatch
{
    private static bool Prefix(SturdyClamp __instance, Creature creature, ref bool __result)
    {
        if (creature != __instance.Owner.Creature || creature.GetPower<FortifyPower>() == null
            || creature.GetPower<BarricadePower>() == null)
            return true;
        // Let native prevention select Barricade regardless of listener order.
        __result = true;
        return false;
    }
}
