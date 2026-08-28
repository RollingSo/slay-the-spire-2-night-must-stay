using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Nodes;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch]
    public static class IroneyeAnimationPatch
    {
        [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
        [HarmonyPostfix]
        public static void AddDistanceIndicator(NCreature __instance)
        {
            if (__instance.Entity?.Player?.Character is not Ironeye
                || __instance.GetNodeOrNull<IroneyeDistanceIndicator>(
                    "IroneyeDistanceIndicator") != null)
            {
                return;
            }

            IroneyeDistanceIndicator indicator =
                IroneyeDistanceIndicator.Create(__instance);
            __instance.AddChild(indicator);

            // NCreature puts its character visuals at child index 0. Keep the
            // Distance readout beside that node, just like the original orb UI,
            // instead of leaving it at the top of the combat UI draw order.
            __instance.MoveChild(indicator, 1);
        }

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
            if (creature.Entity?.Player?.Character is not Ironeye)
                return false;

            rig = creature.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype")!;
            return rig != null && rig.HasMethod("play_trigger");
        }
    }
}
