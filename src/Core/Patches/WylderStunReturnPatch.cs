using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;

// Both public Stun overloads converge here. Observe the successful leaf once,
// await the native command, then await card movement without fire-and-forget.
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Stun), new Type[]
    { typeof(Creature), typeof(Func<IReadOnlyList<Creature>, Task>), typeof(string) })]
internal static class WylderStunReturnPatch
{
    [HarmonyPrefix]
    private static void Capture(Creature creature, out MoveState __state) => __state = creature.Monster?.NextMove;

    [HarmonyPostfix]
    private static void Return(Creature creature, MoveState __state, ref Task __result) =>
        __result = Finish(__result, creature, __state);

    private static async Task Finish(Task native, Creature target, MoveState previous)
    {
        await native;
        if (target.IsAlive && target.IsStunned && !ReferenceEquals(previous, target.Monster?.NextMove))
            await WylderStunReturn.Notify(target);
    }
}
