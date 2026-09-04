#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace NightMustStay.Core.Patches;

/// <summary>
/// Adds the local character's display name and current total floor to the
/// game's existing IN_RUN Steam Rich Presence template. Reusing the game's
/// Character value keeps the status visible without requiring a new Steamworks
/// localization token, which a Workshop mod cannot register for the game.
/// </summary>
[HarmonyPatch]
internal static class SteamRichPresencePatch
{
    private const string CharacterPresenceKey = "Character";
    private const string FloorPresenceKey = "Floor";
    private static readonly PropertyInfo StateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    private static IEnumerable<MethodBase> TargetMethods()
    {
        // UpdateRichPresence covers new and loaded runs. AfterMapLocationChanged
        // refreshes the value whenever the player advances to another floor.
        foreach (string methodName in new[] { "UpdateRichPresence", "AfterMapLocationChanged" })
        {
            MethodInfo? method = AccessTools.DeclaredMethod(typeof(RunManager), methodName);
            if (method is not null)
                yield return method;
        }
    }

    [HarmonyPostfix]
    private static void RefreshCharacterAndFloor(RunManager __instance)
    {
        try
        {
            RunState state = (RunState)StateProperty.GetValue(__instance)!;
            var player = LocalContext.GetMe(state);
            if (player is null)
                return;

            var character = player.Character;
            if (character is null)
                return;

            string characterName = character.Title.GetFormattedText();
            int floor = Math.Max(1, state.TotalFloor);

            LocString floorText = new("characters", "RICH_PRESENCE.floor");
            floorText.Add("Floor", (decimal)floor);

            PlatformUtil.SetRichPresenceValue(
                CharacterPresenceKey,
                $"{characterName} · {floorText.GetFormattedText()}");
            PlatformUtil.SetRichPresenceValue(FloorPresenceKey, floor.ToString());
        }
        catch (Exception exception)
        {
            // Rich Presence is cosmetic and must never interrupt run setup or
            // room transitions on non-Steam platforms or changed game builds.
            Log.Warn($"Night Must Stay could not update Steam Rich Presence: {exception.Message}");
        }
    }
}
