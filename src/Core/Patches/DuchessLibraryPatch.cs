using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches;

[HarmonyPatch]
public static class DuchessLibraryPatch
{
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
    [HarmonyPostfix]
    public static void Ready(NCardLibrary __instance) => EnsureFilter(__instance);
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
    [HarmonyPostfix]
    public static void Open(NCardLibrary __instance) => EnsureFilter(__instance);
    private static void EnsureFilter(NCardLibrary __instance)
    {
        var template = AccessTools.Field(typeof(NCardLibrary), "_ironcladFilter")?.GetValue(__instance) as NCardPoolFilter;
        var filters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(__instance) as IDictionary<NCardPoolFilter, Func<CardModel, bool>>;
        if (template?.GetParent() is not Node parent || filters == null) return;
        var filter = parent.GetNodeOrNull<NCardPoolFilter>("DuchessPool");
        if (filter == null)
        {
            filter = GD.Load<PackedScene>(SceneHelper.GetScenePath("screens/card_library/library_pool_toggle"))
                .Instantiate<NCardPoolFilter>();
            filter.Name = "DuchessPool";
            filter.CustomMinimumSize = template.CustomMinimumSize;
            filter.FocusMode = template.FocusMode;
            parent.AddChild(filter);
            filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(selected =>
                AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter")?.Invoke(__instance, new object[] { selected })));
            var captured = filter;
            filter.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
                AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl")?.SetValue(__instance, captured)));
        }
        filter.Visible = true;
        filter.Loc = new LocString("card_library", "POOL_DUCHESS_TIP");
        var image = filter.GetNodeOrNull<TextureRect>("Image");
        if (image != null)
        {
            image.Texture = GD.Load<Texture2D>("res://duchess_assets/character_icon_duchess.png");
            image.Modulate = Colors.White;
            var shadow = image.GetNodeOrNull<TextureRect>("Shadow");
            if (shadow != null) shadow.Texture = image.Texture;
        }
        filters[filter] = card => ModelDb.CardPool<DuchessCardPool>().AllCardIds.Contains(card.Id);
        var characters = AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters")?.GetValue(__instance) as IDictionary<CharacterModel, NCardPoolFilter>;
        if (characters != null) characters[ModelDb.Character<Duchess>()] = filter;
    }
    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.AssetPaths), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Assets(ref string[] __result) => __result = (__result ?? Array.Empty<string>())
        .Append("res://duchess_assets/character_icon_duchess.png").Distinct().ToArray();
}
