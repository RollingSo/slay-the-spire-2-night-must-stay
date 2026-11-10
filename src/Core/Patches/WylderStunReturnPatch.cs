using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
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
    private static bool Capture(Creature creature, out MoveState __state, ref Task __result)
    {
        __state = creature.Monster?.NextMove;
        // Test Subject survives its first two lethal hits through Adaptable and
        // schedules a respawn move. Stunning the zero-HP creature overwrites that
        // move and leaves the encounter stuck forever.
        if (creature.Monster is TestSubject && creature.CurrentHp <= 0
            && creature.HasPower<AdaptablePower>())
        {
            __result = Task.CompletedTask;
            return false;
        }
        return true;
    }

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
