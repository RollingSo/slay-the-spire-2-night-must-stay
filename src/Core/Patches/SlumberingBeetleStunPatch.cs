using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NightMustStay.Core.Patches;

// Slumber's wake-up is itself a stun callback. A pre-existing uninterruptible
// Imbalance stun can reject that callback after Slumber has already been removed.
[HarmonyPatch(typeof(Creature), nameof(Creature.StunInternal))]
internal static class SlumberingBeetleStunPatch
{
    [HarmonyPrefix]
    private static void BeforeStun(Creature __instance,
        ref Func<IReadOnlyList<Creature>, Task> stunMove)
    {
        if (__instance.Monster is not SlumberingBeetle beetle || beetle.IsAwake)
            return;

        var original = stunMove;
        stunMove = async targets =>
        {
            await original(targets);
            // Keep an intact Slumber asleep, and let the native wake callback
            // run unchanged when it was successfully installed.
            if (!__instance.IsAlive || beetle.IsAwake || __instance.HasPower<SlumberPower>())
                return;

            await beetle.WakeUpMove(targets);
            beetle.NextMove.FollowUpState =
                beetle.MoveStateMachine.States[SlumberingBeetle.rolloutMoveId];
        };
    }
}
