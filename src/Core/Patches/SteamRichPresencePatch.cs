#nullable enable

using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches;

/// <summary>
/// Extends the game's native IN_RUN Rich Presence update for mod characters.
/// The game remains responsible for the display template and run-progress
/// fields; this patch only replaces its model ID value with the localized
/// display name for Night Must Stay characters.
/// </summary>
[HarmonyPatch(typeof(RunManager), "UpdateRichPresence")]
internal static class SteamRichPresencePatch
{
    private const string CharacterPresenceKey = "Character";
    private static readonly PropertyInfo StateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    [HarmonyPostfix]
    private static void AddModCharacterName(RunManager __instance)
    {
        try
        {
            RunState state = (RunState)StateProperty.GetValue(__instance)!;
            var player = LocalContext.GetMe(state);
            var character = player?.Character;
            if (character is not Guardian
                && character is not Ironeye
                && character is not Revenant)
                return;

            PlatformUtil.SetRichPresenceValue(
                CharacterPresenceKey,
                character.Title.GetFormattedText());
        }
        catch (Exception exception)
        {
            // Rich Presence is cosmetic and must never interrupt run startup.
            Log.Warn($"Night Must Stay could not add its character to Steam Rich Presence: {exception.Message}");
        }
    }
}
