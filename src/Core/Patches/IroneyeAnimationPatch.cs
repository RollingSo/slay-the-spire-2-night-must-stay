using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Nodes;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

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
            {
                var mode = SaveManager.Instance.PrefsSave.FastMode;
                // Keep native command waits unchanged; only retime presentation.
                rig.Call("set_playback_mode", mode.ToString());
                string visualTrigger = trigger == "Attack"
                    && IroneyeAnimationContext.For(__instance.Entity) == IroneyeAnimationContext.AttackKind.Knife
                    ? "AttackDagger" : trigger;
                rig.Call("play_trigger", visualTrigger);
            }
        }

        [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
        [HarmonyPrefix]
        internal static void EnterAttack(AttackCommand __instance, out IroneyeAnimationContext.Scope __state)
        {
            __state = IroneyeAnimationContext.Enter(__instance);
        }

        [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
        [HarmonyPostfix]
        internal static void LeaveAttack(IroneyeAnimationContext.Scope __state)
        {
            // Execute's awaited continuations retain their captured AsyncLocal scope.
            // Restore the caller immediately so simultaneous commands cannot leak kinds.
            IroneyeAnimationContext.Restore(__state);
        }

        [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
        [HarmonyFinalizer]
        internal static System.Exception FinalizeAttack(System.Exception __exception, IroneyeAnimationContext.Scope __state)
        {
            if (__exception != null) IroneyeAnimationContext.Restore(__state);
            return __exception;
        }

        [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
        [HarmonyPrefix]
        public static void PrepareTiming(Creature creature, string triggerName, float waitTime)
        {
            if (TestMode.IsOn || creature?.Player?.Character is not Ironeye) return;
            NCreature node = creature.GetCreatureNode();
            if (node != null && TryGetRig(node, out Node rig))
                rig.Call("set_action_timing", triggerName, waitTime);
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
