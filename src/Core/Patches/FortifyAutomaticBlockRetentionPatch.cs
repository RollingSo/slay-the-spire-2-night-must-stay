using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

// Modify only the native automatic-clear assignment. ShouldClearBlock and all
// prevention callbacks remain untouched: Barricade and other preventers win.
[HarmonyPatch]
internal static class FortifyAutomaticBlockRetentionPatch
{
    private static MethodBase TargetMethod()
    {
        MethodInfo clear = AccessTools.Method(typeof(Creature), "ClearBlock");
        Type stateMachine = clear.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            ?? throw new InvalidOperationException("Creature.ClearBlock no longer has its expected async state machine.");
        return AccessTools.Method(stateMachine, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        MethodInfo setter = AccessTools.PropertySetter(typeof(Creature), nameof(Creature.Block));
        MethodInfo retention = AccessTools.Method(typeof(FortifyAutomaticBlockRetentionPatch), nameof(GetAutomaticClearBlock));
        int replacements = 0;
        for (int i = 0; i + 1 < code.Count; i++)
        {
            if (code[i].opcode != OpCodes.Ldc_I4_0 || !code[i + 1].Calls(setter)) continue;
            // Stack was [creature, 0]. Preserve the receiver and calculate the
            // new value without issuing a second clear or a LoseBlock command.
            code[i].opcode = OpCodes.Dup;
            code[i].operand = null;
            code.Insert(i + 1, new CodeInstruction(OpCodes.Call, retention));
            replacements++;
            i++;
        }
        if (replacements != 1) throw new InvalidOperationException("Expected exactly one native automatic Block clear assignment.");
        return code;
    }

    internal static int GetAutomaticClearBlock(Creature creature)
    {
        FortifyPower fortify = creature.GetPower<FortifyPower>();
        if (fortify == null) return 0;
        int retained = fortify.GetAutomaticClearRetainedBlock(creature.Block);
        fortify.NotifyAutomaticClearRetention(retained);
        return retained;
    }
}
