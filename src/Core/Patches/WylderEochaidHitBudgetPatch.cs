using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;

// AttackCommand snapshots ModifyAttackHitCount before the loop. Only this
// card's own resolving command needs a budget that can grow after a real hit.
// Preserve the native loop, target/death checks, Results, history and hooks.
[HarmonyPatch]
internal static class WylderEochaidHitBudgetPatch
{
    private static MethodBase TargetMethod() => AccessTools.Method(StateMachine(), "MoveNext");
    private static Type StateMachine() => AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute))
        .GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var less = AccessTools.Method(typeof(decimal), "op_LessThan", new[] { typeof(decimal), typeof(decimal) });
        // Fail explicitly on an incompatible engine layout, rather than silently
        // shipping a card that stops at X or changes unrelated comparisons.
        if (codes.Count(code => code.Calls(less)) != 1)
            throw new InvalidOperationException("Eochaid: expected one native decimal hit-loop comparison.");
        var command = AccessTools.Field(StateMachine(), "<>4__this");
        foreach (var code in codes)
        {
            if (!code.Calls(less)) { yield return code; continue; }
            var receiver = new CodeInstruction(OpCodes.Ldarg_0);
            receiver.labels.AddRange(code.labels);
            receiver.blocks.AddRange(code.blocks);
            yield return receiver;
            yield return new CodeInstruction(OpCodes.Ldfld, command);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WylderEochaidHitBudgetPatch), nameof(Continue)));
        }
    }

    private static bool Continue(decimal index, decimal nativeHitCount, AttackCommand command) =>
        index < (command.ModelSource is WylderEochaidsDancingBlade card && card.DoublesHitBudget(command)
            ? nativeHitCount * 2m : command.ModelSource is WylderOrdovisVortex ordovis && ordovis.AddsHits(command)
                ? nativeHitCount + ordovis.DynamicVars.Repeat.BaseValue : nativeHitCount);
}
