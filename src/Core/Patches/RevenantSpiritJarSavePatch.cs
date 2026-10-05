using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Potions;

namespace NightMustStay.Core.Patches;

// Native SerializablePotion has no custom properties on either supported branch.
// Use a save-only relic envelope: native JSON and multiplayer packets already
// preserve its SavedProperties. It never enters the live relic inventory or UI.
public sealed class RevenantSpiritJarSaveData : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.None;
    [SavedProperty]
    public string CapturedSpiritJarsV1 { get; set; } = "";
}

public static class RevenantSpiritJarPersistence
{
    public sealed record Capture(int Slot, string Category, string Entry);
    private const string PropertyName = nameof(RevenantSpiritJarSaveData.CapturedSpiritJarsV1);
    public static ModelId EnvelopeId => ModelDb.Relic<RevenantSpiritJarSaveData>().Id;

    public static SerializableRelic CreateEnvelope(IReadOnlyList<PotionModel> slots)
    {
        Capture[] captures = slots.Select((potion, slot) => (potion, slot))
            .Where(item => item.potion is SpiritCallingJar)
            .Select(item => new Capture(item.slot, ((SpiritCallingJar)item.potion).CapturedMonsterCategory,
                ((SpiritCallingJar)item.potion).CapturedMonsterEntry)).ToArray();
        if (captures.Length == 0) return null;
        return new SerializableRelic
        {
            Id = EnvelopeId,
            Props = new SavedProperties
            {
                strings = new() { new SavedProperties.SavedProperty<string>(PropertyName, JsonSerializer.Serialize(captures)) },
            },
        };
    }

    public static void Restore(IReadOnlyList<PotionModel> slots, IEnumerable<SerializableRelic> envelopes)
    {
        foreach (SerializableRelic envelope in envelopes)
        {
            string json = envelope.Props?.strings?.FirstOrDefault(property => property.name == PropertyName).value;
            if (string.IsNullOrWhiteSpace(json)) continue;
            try
            {
                foreach (Capture capture in JsonSerializer.Deserialize<Capture[]>(json) ?? Array.Empty<Capture>())
                    if (capture != null && capture.Slot >= 0 && capture.Slot < slots.Count
                        && slots[capture.Slot] is SpiritCallingJar jar
                        && capture.Category == ModelId.SlugifyCategory<MonsterModel>()
                        && !string.IsNullOrWhiteSpace(capture.Entry))
                        jar.SetCapturedMonster(new ModelId(capture.Category, capture.Entry));
            }
            catch (JsonException)
            {
                Log.Warn("Invalid captured-spirit jar save data; affected jars remain unusable instead of summoning the wrong monster.");
            }
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ToSerializable))]
public static class RevenantSpiritJarSavePatch
{
    [HarmonyPostfix]
    public static void SaveCapturedMonsters(Player __instance, SerializablePlayer __result)
    {
        SerializableRelic envelope = RevenantSpiritJarPersistence.CreateEnvelope(__instance.PotionSlots);
        if (envelope != null) __result.Relics.Add(envelope);
    }
}

[HarmonyPatch]
public static class RevenantSpiritJarLoadPatch
{
    public sealed record LoadState(List<SerializableRelic> OriginalRelics, SerializableRelic[] Envelopes);

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Player), "LoadInventory");
        yield return AccessTools.Method(typeof(Player), nameof(Player.SyncWithSerializedPlayer));
    }

    [HarmonyPrefix]
    public static void ExcludeSaveOnlyEnvelope(SerializablePlayer __0, out LoadState __state)
    {
        SerializableRelic[] envelopes = __0.Relics.Where(relic => relic.Id == RevenantSpiritJarPersistence.EnvelopeId).ToArray();
        __state = new LoadState(__0.Relics, envelopes);
        if (envelopes.Length > 0)
            __0.Relics = __0.Relics.Except(envelopes).ToList();
    }

    [HarmonyPostfix]
    public static void RestoreCapturedMonsters(Player __instance, LoadState __state) =>
        RevenantSpiritJarPersistence.Restore(__instance.PotionSlots, __state.Envelopes);

    [HarmonyFinalizer]
    public static Exception PreserveInputSnapshot(SerializablePlayer __0, LoadState __state, Exception __exception)
    {
        if (__state != null) __0.Relics = __state.OriginalRelics;
        return __exception;
    }
}

// Run history displays serialized relics directly; exclude the envelope there
// as well so this implementation detail never appears as a collectible relic.
[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public static class RevenantSpiritJarHistoryPatch
{
    [HarmonyPrefix]
    public static void HideEnvelope(SerializableRun __0, out List<(SerializablePlayer Player, List<SerializableRelic> Relics)> __state)
    {
        __state = new();
        foreach (SerializablePlayer player in __0.Players)
            if (player.Relics.Any(relic => relic.Id == RevenantSpiritJarPersistence.EnvelopeId))
            {
                __state.Add((player, player.Relics));
                player.Relics = player.Relics.Where(relic => relic.Id != RevenantSpiritJarPersistence.EnvelopeId).ToList();
            }
    }

    [HarmonyFinalizer]
    public static Exception PreserveRunSnapshot(List<(SerializablePlayer Player, List<SerializableRelic> Relics)> __state, Exception __exception)
    {
        if (__state != null)
            foreach (var (player, relics) in __state) player.Relics = relics;
        return __exception;
    }
}
