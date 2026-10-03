#nullable enable
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Helpers;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Patches;

[HarmonyPatch]
public static class RevenantAnimationPatch
{
    // Preserve routing across all hits of a card, but not across cards.
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
    [HarmonyPrefix]
    public static void BeforeCardMotion(CardPlay __1) => ClearCardMotion(__1.Card);

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
    [HarmonyPrefix]
    public static void AfterCardMotion(CardPlay __2) => ClearCardMotion(__2.Card);

    private static void ClearCardMotion(CardModel card)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive) return;
        if (card.Owner?.Creature != null
            && NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature) is { } creature
            && TryGetRig(creature, out Node rig))
            rig.Call("clear_card_motion");
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    public static void CreatureReady(NCreature __instance)
    {
        RevenantSummonManager.NotifyCreatureNodeReady(__instance);
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

    public static void PlayCardMotion(CardModel card, string motion)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive) return;
        if (card.Owner?.Creature == null)
            return;
        NCreature? creature = NCombatRoom.Instance?.GetCreatureNode(card.Owner.Creature);
        if (creature != null && TryGetRig(creature, out Node rig))
        {
            rig.Call("play_card_motion", motion);
        }
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
