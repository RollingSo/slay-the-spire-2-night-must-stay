using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(NRunHistoryPlayerIcon), nameof(NRunHistoryPlayerIcon.LoadRun))]
public static class DuchessHistoryIconPatch
{
    private sealed record OriginalLayout(TextureRect.ExpandModeEnum Expand, TextureRect.StretchModeEnum Stretch);
    private static readonly ConditionalWeakTable<TextureRect, OriginalLayout> OriginalLayouts = new();

    [HarmonyPostfix]
    public static void PreserveAspectRatio(NRunHistoryPlayerIcon __instance)
    {
        var icon = __instance.GetNodeOrNull<TextureRect>("%Icon");
        if (icon == null) return;
        if (SaveUtil.CharacterOrDeprecated(__instance.Player.Character) is Duchess)
        {
            OriginalLayouts.GetValue(icon, item => new OriginalLayout(item.ExpandMode, item.StretchMode));
            icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }
        else if (OriginalLayouts.TryGetValue(icon, out var original))
        {
            // A history icon can be reused; do not alter other characters.
            icon.ExpandMode = original.Expand;
            icon.StretchMode = original.Stretch;
            OriginalLayouts.Remove(icon);
        }
    }
}
