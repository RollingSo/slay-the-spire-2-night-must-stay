using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate))]
internal static class MonsterScriptedStunTransitionPatch
{
    [HarmonyPrefix]
    private static void PrioritizeScriptedMove(MonsterModel __instance, MoveState state,
        ref bool forceTransition)
    {
        if (forceTransition || __instance.NextMove is not { Id: "STUNNED", CanTransitionAway: false })
            return;

        forceTransition = __instance switch
        {
            DecimillipedeSegment segment => ReferenceEquals(state, segment.DeadState),
            CeremonialBeast beast => beast.IsInSecondPhase
                && state.Id == "STUNNED"
                && state.FollowUpStateId is { } followUp
                && followUp == beast.BeastCryState?.StateId,
            _ => false,
        };
    }
}
