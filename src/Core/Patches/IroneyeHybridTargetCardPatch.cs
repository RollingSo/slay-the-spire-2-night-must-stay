using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Patches;

internal static class IroneyeHybridTargetState
{
    private static readonly HashSet<ulong> ActiveCardPlays = new();

    public static bool IsActive => ActiveCardPlays.Count > 0;

    public static bool IsHybridTargetCard(CardModel card) =>
        card is AdvanceAndRetreat or IroneyeSwift;

    public static void Begin(NCardPlay play)
    {
        if (play.Holder?.CardModel is { } card && IsHybridTargetCard(card))
            ActiveCardPlays.Add(play.GetInstanceId());
    }

    public static void End(NCardPlay play) => ActiveCardPlays.Remove(play.GetInstanceId());

    public static bool IsValid(Creature creature, CardModel card) =>
        creature.IsAlive
        && (creature.Side != card.Owner.Creature.Side
            || creature == card.Owner.Creature);
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
internal static class AdvanceAndRetreatTargetValidationPatch
{
    [HarmonyPrefix]
    private static bool BeforeIsValidTarget(
        CardModel __instance,
        Creature target,
        ref bool __result)
    {
        if (!IroneyeHybridTargetState.IsHybridTargetCard(__instance))
            return true;

        __result = target != null && IroneyeHybridTargetState.IsValid(target, __instance);
        return false;
    }
}

[HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
internal static class AdvanceAndRetreatTargetManagerPatch
{
    [HarmonyPrefix]
    private static bool BeforeAllowedToTargetCreature(Creature creature, ref bool __result)
    {
        if (!IroneyeHybridTargetState.IsActive)
            return true;

        Creature local = LocalContext.GetMe(creature.CombatState)?.Creature;
        __result = creature.IsAlive
            && (creature.Side == MegaCrit.Sts2.Core.Combat.CombatSide.Enemy
                || creature == local);
        return false;
    }
}

[HarmonyPatch(typeof(NMouseCardPlay), nameof(NMouseCardPlay.Start))]
internal static class AdvanceAndRetreatMouseStartPatch
{
    [HarmonyPrefix]
    private static void BeforeStart(NMouseCardPlay __instance) =>
        IroneyeHybridTargetState.Begin(__instance);
}

[HarmonyPatch(typeof(NMouseCardPlay), "_ExitTree")]
internal static class AdvanceAndRetreatMouseExitPatch
{
    [HarmonyPostfix]
    private static void AfterExitTree(NMouseCardPlay __instance) =>
        IroneyeHybridTargetState.End(__instance);
}

[HarmonyPatch(typeof(NControllerCardPlay), nameof(NControllerCardPlay.Start))]
internal static class AdvanceAndRetreatControllerStartPatch
{
    [HarmonyPrefix]
    private static void BeforeStart(NControllerCardPlay __instance) =>
        IroneyeHybridTargetState.Begin(__instance);
}

[HarmonyPatch(typeof(NControllerCardPlay), "_ExitTree")]
internal static class AdvanceAndRetreatControllerExitPatch
{
    [HarmonyPostfix]
    private static void AfterExitTree(NControllerCardPlay __instance) =>
        IroneyeHybridTargetState.End(__instance);
}

[HarmonyPatch(typeof(NControllerCardPlay), "SingleCreatureTargeting")]
internal static class AdvanceAndRetreatControllerTargetingPatch
{
    private static readonly MethodInfo TryPlayCardMethod =
        AccessTools.Method(typeof(NCardPlay), "TryPlayCard");

    [HarmonyPrefix]
    private static bool BeforeSingleCreatureTargeting(
        NControllerCardPlay __instance,
        ref Task __result)
    {
        if (__instance.Holder?.CardModel is not { } card
            || !IroneyeHybridTargetState.IsHybridTargetCard(card))
            return true;

        __result = RunHybridTargeting(__instance, card);
        return false;
    }

    private static async Task RunHybridTargeting(
        NControllerCardPlay play,
        CardModel card)
    {
        List<NCreature> nodes = card.CombatState.GetOpponentsOf(card.Owner.Creature)
            .Where(creature => creature.IsHittable)
            .Prepend(card.Owner.Creature)
            .Select(creature => NCombatRoom.Instance.GetCreatureNode(creature))
            .OfType<NCreature>()
            .ToList();
        if (nodes.Count == 0)
        {
            play.CancelPlayCard();
            return;
        }

        NTargetManager manager = NTargetManager.Instance;
        manager.StartTargeting(
            TargetType.AnyEnemy,
            play.Holder.CardNode,
            TargetMode.Controller,
            () => !GodotObject.IsInstanceValid(play),
            null);
        NCombatRoom.Instance.RestrictControllerNavigation(nodes.Select(node => node.Hitbox));
        nodes[0].Hitbox.TryGrabFocus();

        Node selected = await manager.SelectionFinished();
        NCombatRoom.Instance.EnableControllerNavigation();
        if (!GodotObject.IsInstanceValid(play) || selected == null)
            return;

        Creature target = selected switch
        {
            NCreature creatureNode => creatureNode.Entity,
            NMultiplayerPlayerState playerState => playerState.Player.Creature,
            _ => null,
        };
        if (target == null)
        {
            play.CancelPlayCard();
            return;
        }

        TryPlayCardMethod.Invoke(play, new object[] { target });
    }
}
