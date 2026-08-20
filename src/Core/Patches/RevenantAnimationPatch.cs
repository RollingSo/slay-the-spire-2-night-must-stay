using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using sts2mod.Core.Models.Characters;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Patches;

[HarmonyPatch]
public static class RevenantAnimationPatch
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    [HarmonyPostfix]
    public static void SetAnimationTrigger(NCreature __instance, string trigger)
    {
        if (TryGetRig(__instance, out Node rig))
            rig.Call("play_trigger", trigger);
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    [HarmonyPrefix]
    public static void StartDeathAnim(NCreature __instance)
    {
        RevenantSummonManager.NotifyCreatureDeath(__instance.Entity);
        if (TryGetRig(__instance, out Node rig))
            rig.Call("play_trigger", "Dead");
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
    [HarmonyPostfix]
    public static void StartReviveAnim(NCreature __instance)
    {
        if (TryGetRig(__instance, out Node rig))
            rig.Call("play_trigger", "Revive");
    }

    private static bool TryGetRig(NCreature creature, out Node rig)
    {
        rig = null!;
        if (creature.Entity?.Player?.Character is not Revenant)
            return false;

        rig = creature.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype")!;
        return rig != null && rig.HasMethod("play_trigger");
    }
}
