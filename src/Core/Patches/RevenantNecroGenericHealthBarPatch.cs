using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Patches;

/// <summary>
/// Revived enemies keep their original creature visuals, but their player-side
/// health UI must be consistent. Normalize family and Necros to one compact bar
/// instead of inheriting monster-specific bounds and HP-bar reductions.
/// </summary>
[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.UpdateLayoutForCreatureBounds))]
public static class RevenantNecroGenericHealthBarPatch
{
    private const float GenericWidth = 180f;
    private static readonly FieldInfo CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");
    private static readonly FieldInfo BlockContainerField =
        AccessTools.Field(typeof(NHealthBar), "_blockContainer");
    private static readonly FieldInfo OriginalBlockPositionField =
        AccessTools.Field(typeof(NHealthBar), "_originalBlockPosition");

    private static void Postfix(NHealthBar __instance, Control bounds)
    {
        Creature creature = CreatureField.GetValue(__instance) as Creature;
        // The native layout defers its final width write.  Defer ours as well
        // so the generic Necro layout is the last authoritative update.
        Callable.From(() => ApplyGenericLayout(__instance, bounds, creature)).CallDeferred();
    }

    private static bool IsRevenantSummon(Creature creature) =>
        creature != null && (RevenantSummonManager.IsRegisteredNecroCreature(creature)
            || (creature.PetOwner?.Character is NightMustStay.Core.Models.Characters.Revenant
                && RevenantSummonManager.For(creature.PetOwner).IsKnownFamilyCreature(creature)));

    public static void Refresh(NCreature node)
    {
        if (node?.Visuals?.Bounds == null) return;
        foreach (Node child in node.FindChildren("*", "", true, false))
            if (child is NHealthBar bar)
                bar.UpdateLayoutForCreatureBounds(node.Visuals.Bounds);
    }

    private static void ApplyGenericLayout(NHealthBar healthBar, Control bounds, Creature creature)
    {
        if (!IsRevenantSummon(creature) || !GodotObject.IsInstanceValid(healthBar) || !GodotObject.IsInstanceValid(bounds))
            return;

        healthBar.UpdateWidthRelativeToReferenceValue(Math.Max(1, creature.MaxHp), GenericWidth);
        float left = bounds.GlobalPosition.X + bounds.Size.X * bounds.GetGlobalTransform().Scale.X * 0.5f
            - GenericWidth * healthBar.HpBarContainer.GetGlobalTransform().Scale.X * 0.5f;
        healthBar.HpBarContainer.GlobalPosition = new Vector2(left, healthBar.HpBarContainer.GlobalPosition.Y);

        if (BlockContainerField.GetValue(healthBar) is not Control blockContainer)
            return;
        blockContainer.GlobalPosition = new Vector2(
            left - blockContainer.Size.X * blockContainer.GetGlobalTransform().Scale.X * 0.5f,
            blockContainer.GlobalPosition.Y);
        OriginalBlockPositionField.SetValue(healthBar, blockContainer.Position);
    }
}
