using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Patches;

[HarmonyPatch]
public static class GuardCounterHealthBarPatch
{
    private const string CounterOverlayName = "GuardianCounterForeground";
    private const string HiddenPoisonOverlayName = "IroneyeHiddenPoisonForeground";

    // Warm ochre distinguishes Guard Counter damage from the native poison,
    // doom, and Block-blue health-bar forecasts.
    private static readonly Color CounterColor = new Color("C49A4A");
    private static readonly Color HiddenPoisonColor = new Color("B7D629");
    private static readonly FieldInfo CreatureField = AccessTools.Field(typeof(NHealthBar), "_creature");
    private static readonly FieldInfo HpForegroundField = AccessTools.Field(typeof(NHealthBar), "_hpForeground");
    private static readonly FieldInfo PoisonForegroundField = AccessTools.Field(typeof(NHealthBar), "_poisonForeground");
    private static readonly FieldInfo HpForegroundContainerField = AccessTools.Field(typeof(NHealthBar), "_hpForegroundContainer");
    private static readonly FieldInfo ExpectedMaxWidthField = AccessTools.Field(typeof(NHealthBar), "_expectedMaxFgWidth");

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(NHealthBar), "RefreshForeground");
    }

    [HarmonyPostfix]
    private static void AfterRefreshForeground(NHealthBar __instance)
    {
        Creature enemy = CreatureField.GetValue(__instance) as Creature;
        Control hpForeground = HpForegroundField.GetValue(__instance) as Control;
        NinePatchRect poisonForeground = PoisonForegroundField.GetValue(__instance) as NinePatchRect;
        Control foregroundContainer = HpForegroundContainerField.GetValue(__instance) as Control;
        if (enemy == null || hpForeground == null || poisonForeground == null || foregroundContainer == null)
        {
            return;
        }

        NinePatchRect counterOverlay = GetOrCreateOverlay(
            hpForeground,
            poisonForeground,
            CounterOverlayName,
            CounterColor);
        NinePatchRect hiddenPoisonOverlay = GetOrCreateOverlay(
            hpForeground,
            poisonForeground,
            HiddenPoisonOverlayName,
            HiddenPoisonColor);
        counterOverlay.Visible = false;
        hiddenPoisonOverlay.Visible = false;

        if (!enemy.IsAlive || enemy.HpDisplay.IsInfinite() || enemy.CombatState == null)
        {
            return;
        }

        float expectedMaxWidth = (float)ExpectedMaxWidthField.GetValue(__instance);
        float maxWidth = expectedMaxWidth > 0f ? expectedMaxWidth : foregroundContainer.Size.X;
        if (maxWidth <= 0f || enemy.MaxHp <= 0)
            return;

        int poisonDamage = enemy.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
        int hpAfterPoison = Math.Max(0, enemy.CurrentHp - poisonDamage);
        int hiddenPoisonDamage = Math.Max(
            0,
            decimal.ToInt32(enemy.GetPower<HiddenPoisonPower>()?.Amount ?? 0m));
        int hpAfterStartDamage = hpAfterPoison;

        // Hidden Poison resolves at the creature's turn start, immediately after
        // the native Poison forecast segment. Give it its own IronEye-colored
        // segment while keeping the native Poison section intact.
        if (hpAfterPoison > 0 && hiddenPoisonDamage > 0)
        {
            bool hiddenBaseHpWasVisible = hpForeground.Visible;
            hpAfterStartDamage = Math.Max(0, hpAfterPoison - hiddenPoisonDamage);
            hiddenPoisonOverlay.OffsetRight =
                GetForegroundWidth(enemy, hpAfterPoison, maxWidth) - maxWidth;
            if (hpAfterStartDamage <= 0)
            {
                hiddenPoisonOverlay.OffsetLeft = 0f;
                hpForeground.Visible = false;
            }
            else
            {
                float remainingWidth = GetForegroundWidth(enemy, hpAfterStartDamage, maxWidth);
                hpForeground.OffsetRight = remainingWidth - maxWidth;
                hpForeground.Visible = hiddenBaseHpWasVisible;
                hiddenPoisonOverlay.OffsetLeft = Math.Max(
                    0f,
                    remainingWidth - hiddenPoisonOverlay.PatchMarginLeft);
            }
            hiddenPoisonOverlay.Visible = true;
        }

        if (!enemy.IsMonster || hpAfterStartDamage <= 0)
            return;

        Creature guardian = LocalContext.GetMe(enemy.CombatState)?.Creature;
        GuardCounterPower counter = guardian?.GetPower<GuardCounterPower>();
        if (counter == null)
        {
            return;
        }

        int triggerCount = counter.CalculatePreviewTriggerCount(enemy);
        int damagePerTrigger = counter.CalculatePreviewDamagePerTrigger(enemy);
        if (triggerCount <= 0 || damagePerTrigger <= 0)
        {
            return;
        }

        int projectedEnemyBlock = Hook.ShouldClearBlock(enemy.CombatState, enemy, out AbstractModel _)
            ? 0
            : enemy.Block;
        int counterHpDamage = Math.Max(0, damagePerTrigger * triggerCount - projectedEnemyBlock);
        if (counterHpDamage <= 0)
        {
            return;
        }

        bool baseHpWasVisible = hpForeground.Visible;
        int hpAfterCounter = Math.Max(0, hpAfterStartDamage - counterHpDamage);
        counterOverlay.OffsetRight =
            GetForegroundWidth(enemy, hpAfterStartDamage, maxWidth) - maxWidth;

        if (hpAfterCounter <= 0)
        {
            counterOverlay.OffsetLeft = 0f;
            hpForeground.Visible = false;
        }
        else
        {
            float remainingWidth = GetForegroundWidth(enemy, hpAfterCounter, maxWidth);
            hpForeground.OffsetRight = remainingWidth - maxWidth;
            hpForeground.Visible = baseHpWasVisible;
            counterOverlay.OffsetLeft = Math.Max(
                0f,
                remainingWidth - counterOverlay.PatchMarginLeft);
        }

        counterOverlay.Visible = true;
    }

    private static NinePatchRect GetOrCreateOverlay(
        Control hpForeground,
        NinePatchRect poisonForeground,
        string overlayName,
        Color color)
    {
        Node parent = hpForeground.GetParent();
        NinePatchRect overlay = parent.GetNodeOrNull<NinePatchRect>(overlayName);
        if (overlay != null)
        {
            return overlay;
        }

        overlay = (NinePatchRect)poisonForeground.Duplicate();
        overlay.Name = overlayName;
        overlay.UniqueNameInOwner = false;
        overlay.Material = null;
        overlay.Modulate = Colors.White;
        overlay.SelfModulate = color;
        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        overlay.Visible = false;
        parent.AddChild(overlay);
        parent.MoveChild(overlay, hpForeground.GetIndex());
        return overlay;
    }

    private static float GetForegroundWidth(Creature creature, int hp, float maxWidth)
    {
        float width = (float)hp / creature.MaxHp * maxWidth;
        return Math.Max(width, creature.CurrentHp > 0 ? 12f : 0f);
    }
}

[HarmonyPatch]
internal static class HiddenPoisonHealthBarTextPatch
{
    private static readonly FieldInfo CreatureField =
        AccessTools.Field(typeof(NHealthBar), "_creature");
    private static readonly FieldInfo HpLabelField =
        AccessTools.Field(typeof(NHealthBar), "_hpLabel");

    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NHealthBar), "RefreshText");

    [HarmonyPostfix]
    private static void AfterRefreshText(NHealthBar __instance)
    {
        Creature creature = CreatureField.GetValue(__instance) as Creature;
        MegaLabel hpLabel = HpLabelField.GetValue(__instance) as MegaLabel;
        if (creature == null
            || hpLabel == null
            || creature.CurrentHp <= 0
            || creature.HpDisplay.IsInfinite())
        {
            return;
        }

        int poisonDamage = creature.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
        int hiddenPoisonDamage = Math.Max(
            0,
            decimal.ToInt32(creature.GetPower<HiddenPoisonPower>()?.Amount ?? 0m));
        if (poisonDamage < creature.CurrentHp
            && hiddenPoisonDamage > 0
            && poisonDamage + hiddenPoisonDamage >= creature.CurrentHp)
        {
            hpLabel.AddThemeColorOverride(
                ThemeConstants.Label.FontColor,
                new Color("D9F05A"));
            hpLabel.AddThemeColorOverride(
                ThemeConstants.Label.FontOutlineColor,
                new Color("304200"));
        }
    }
}
