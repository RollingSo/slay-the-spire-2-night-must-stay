using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches;

/// <summary>
/// The base game forwards creature animation triggers only to Spine. This
/// bridge gives the Guardian's Skeleton2D visual the same combat events.
/// </summary>
[HarmonyPatch]
public static class GuardianAnimationPatch
{
    public static void PlayGuardCounter(Creature owner)
    {
        NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (creatureNode != null && TryGetGuardianRig(creatureNode, out var rig))
            rig.Call("play_trigger", "GuardCounter");
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    [HarmonyPostfix]
    public static void SetAnimationTrigger(NCreature __instance, string trigger)
    {
        if (TryGetGuardianRig(__instance, out var rig))
            rig.Call("play_trigger", trigger);
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    [HarmonyPrefix]
    public static void StartDeathAnim(NCreature __instance)
    {
        if (TryGetGuardianRig(__instance, out var rig))
            rig.Call("play_trigger", "Dead");
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
    [HarmonyPostfix]
    public static void StartReviveAnim(NCreature __instance)
    {
        if (TryGetGuardianRig(__instance, out var rig))
            rig.Call("play_trigger", "Revive");
    }

    private static bool TryGetGuardianRig(NCreature creature, out Node rig)
    {
        rig = null!;
        if (creature.Entity?.Player?.Character is not Guardian)
            return false;

        rig = creature.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype")!;
        return rig != null && rig.HasMethod("play_trigger");
    }
}
