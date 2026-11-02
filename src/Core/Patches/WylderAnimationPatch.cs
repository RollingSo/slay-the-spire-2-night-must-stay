using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Nodes;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch]
    public static class WylderAnimationPatch
    {
        [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
        [HarmonyPostfix]
        public static void AttachStatusVisual(NCreature __instance)
        {
            if (__instance.Entity?.Player?.Character is Wylder
                && __instance.GetNodeOrNull<Node>("WylderStatusVisual") == null)
                __instance.AddChild(new NightMustStay.Core.Nodes.Vfx.WylderStatusVisual {
                    Name = "WylderStatusVisual", CreatureNode = __instance });
        }

        public static void PlayBlock(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
        {
            if (creature.GetCreatureNode() is { } node && TryGetRig(node, out Node rig))
            {
                rig.Call("play_trigger", "Block");
            }
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
            if (creature.Entity?.Player?.Character is not Wylder)
                return false;

            rig = creature.Visuals?.GetNodeOrNull<Node>("Visuals/Prototype")!;
            if (rig == null || !rig.HasMethod("play_trigger")) return false;
            // Synchronize before a trigger as well as on frames, avoiding stale
            // idle art when entry/removal and animation happen in the same frame.
            if (rig.HasMethod("set_pursuit_active"))
                rig.Call("set_pursuit_active",
                    NightMustStay.Core.Models.Power.WylderPursuitPower.IsActive(creature.Entity));
            return true;
        }
    }
}
