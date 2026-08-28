using System.Linq;
using System;
using System.Collections.Generic;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
    public static class GuardianProgressLoadPatch
    {
        public static void Postfix(ProgressSaveManager __instance)
        {
            GuardianProgressHelper.EnsureGuardianProgress(__instance.Progress);
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.UpdateProgressAfterCombatWon))]
    public static class GuardianProgressPatch
    {
        public static bool Prefix(Player localPlayer, CombatRoom combatRoom)
        {
            if (localPlayer.Character is not Guardian
                && localPlayer.Character is not Ironeye
                && localPlayer.Character is not Revenant)
                return true;

            ProgressState progress = SaveManager.Instance.Progress;
            GuardianProgressHelper.EnsureGuardianProgress(progress);
            ModelId characterId = localPlayer.Character.Id;
            ModelId encounterId = combatRoom.CombatState.Encounter.Id;
            EncounterStats encounterStats = progress.GetOrCreateEncounterStats(encounterId);
            FightStats encounterFight = encounterStats.FightStats.FirstOrDefault(fight => fight.Character == characterId);
            if (encounterFight == null)
            {
                Log.Info($"{characterId} fought {encounterId} for the first time and WON >:)");
                encounterStats.FightStats.Add(new FightStats
                {
                    Character = characterId,
                    Wins = 1,
                    Losses = 0,
                });
            }
            else
            {
                encounterStats.IncrementWin(characterId);
            }

            foreach (var monstersWithSlot in combatRoom.Encounter.MonstersWithSlots)
            {
                ModelId monsterId = monstersWithSlot.Item1.Id;
                EnemyStats enemyStats = progress.GetOrCreateEnemyStats(monsterId);
                bool firstTimeSeen = enemyStats.FightStats.Count == 0;
                FightStats enemyFight = enemyStats.FightStats.FirstOrDefault(fight => fight.Character == characterId);
                if (enemyFight == null)
                {
                    Log.Info($"{characterId} fought {monsterId} for the first time and WON >:(");
                    enemyStats.FightStats.Add(new FightStats
                    {
                        Character = characterId,
                        Wins = 1,
                        Losses = 0,
                    });
                    if (firstTimeSeen)
                    {
                        localPlayer.DiscoveredEnemies.Add(monsterId);
                    }
                }
                else
                {
                    enemyStats.IncrementWin(characterId);
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
    public static class GuardianStatsGridPatch
    {
        public static void Prefix()
        {
            GuardianProgressHelper.EnsureGuardianProgress(SaveManager.Instance.Progress);
        }

        public static void Postfix(NGeneralStatsGrid __instance)
        {
            Control container = AccessTools.Field(typeof(NGeneralStatsGrid), "_characterStatContainer")?.GetValue(__instance) as Control;
            if (container == null)
                return;

            CharacterModel[] modCharacters =
            {
                ModelDb.Character<Guardian>(),
                ModelDb.Character<Ironeye>(),
                ModelDb.Character<Revenant>(),
            };
            foreach (CharacterModel character in modCharacters)
            {
                CharacterStats stats =
                    SaveManager.Instance.Progress.GetStatsForCharacter(character.Id);
                if (stats != null)
                    container.AddChild(NCharacterStats.Create(stats));
            }
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
    public static class GuardianCardLibraryPatch
    {
        public static void Postfix(NCardLibrary __instance)
        {
            GuardianProgressHelper.EnsureGuardianProgress(SaveManager.Instance.Progress);
            EnsureGuardianCardLibraryFilter(__instance);
        }

        internal static void EnsureGuardianCardLibraryFilter(NCardLibrary library)
        {
            var filters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")?.GetValue(library) as IDictionary<NCardPoolFilter, Func<CardModel, bool>>;
            NCardPoolFilter ironcladFilter = AccessTools.Field(typeof(NCardLibrary), "_ironcladFilter")?.GetValue(library) as NCardPoolFilter;
            if (filters == null || ironcladFilter == null)
                return;

            NCardPoolFilter guardianFilter =
                GuardianCardLibraryFilterFactory.FindOrCreateGuardianPoolFilter(library, ironcladFilter);
            if (guardianFilter != null && !filters.ContainsKey(guardianFilter))
            {
                filters[guardianFilter] = card => card.Pool is GuardianCardPool;
            }

            var characterFilters = AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters")?.GetValue(library) as IDictionary<CharacterModel, NCardPoolFilter>;
            if (characterFilters != null && guardianFilter != null)
            {
                characterFilters[ModelDb.Character<Guardian>()] = guardianFilter;
            }

            EnsureIroneyeCardLibraryFilter(library, ironcladFilter, filters, characterFilters);
            EnsureRevenantCardLibraryFilter(library, ironcladFilter, filters, characterFilters);
        }

        private static void EnsureIroneyeCardLibraryFilter(
            NCardLibrary library,
            NCardPoolFilter template,
            IDictionary<NCardPoolFilter, Func<CardModel, bool>> filters,
            IDictionary<CharacterModel, NCardPoolFilter> characterFilters)
        {
            NCardPoolFilter ironeyeFilter =
                GuardianCardLibraryFilterFactory.FindOrCreateIroneyePoolFilter(library, template);
            if (ironeyeFilter == null)
                return;

            if (!filters.ContainsKey(ironeyeFilter))
                filters[ironeyeFilter] = card => card.Pool is IroneyeCardPool;

            if (characterFilters != null)
                characterFilters[ModelDb.Character<Ironeye>()] = ironeyeFilter;
        }

        private static void EnsureRevenantCardLibraryFilter(
            NCardLibrary library,
            NCardPoolFilter template,
            IDictionary<NCardPoolFilter, Func<CardModel, bool>> filters,
            IDictionary<CharacterModel, NCardPoolFilter> characterFilters)
        {
            NCardPoolFilter revenantFilter =
                GuardianCardLibraryFilterFactory.FindOrCreateRevenantPoolFilter(library, template);
            if (revenantFilter == null)
                return;

            // Resolve membership by the pool's authoritative card IDs.  Some
            // canonical CardModel instances can cache Pool before mod pools
            // are appended to ModelDb.AllCardPools, which made the Revenant
            // button select correctly while its predicate matched no cards.
            filters[revenantFilter] = card =>
                ModelDb.CardPool<RevenantCardPool>().AllCardIds.Contains(card.Id);

            if (characterFilters != null)
                characterFilters[ModelDb.Character<Revenant>()] = revenantFilter;
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
    public static class GuardianCardLibraryOpenPatch
    {
        public static void Prefix(NCardLibrary __instance)
        {
            GuardianCardLibraryPatch.EnsureGuardianCardLibraryFilter(__instance);
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.AssetPaths), MethodType.Getter)]
    public static class RevenantCardLibraryAssetsPatch
    {
        public static void Postfix(ref string[] __result)
        {
            __result = (__result ?? Array.Empty<string>())
                .Concat(new[]
                {
                    "res://revenant_assets/character_icon_revenant.png",
                    "res://revenant_assets/character_icon_revenant_outline.png",
                })
                .Distinct()
                .ToArray();
        }
    }

    internal static class GuardianCardLibraryFilterFactory
    {
        public static NCardPoolFilter FindOrCreateGuardianPoolFilter(NCardLibrary library, NCardPoolFilter template)
        {
            Node parent = template.GetParent();
            if (parent == null)
            {
                Log.Warn("Guardian card library filter: Ironclad filter has no parent.");
                return null;
            }

            NCardPoolFilter existing = parent.GetNodeOrNull<NCardPoolFilter>("GuardianPool");
            if (existing != null)
            {
                existing.Visible = true;
                ApplyGuardianPortrait(existing);
                return existing;
            }

            PackedScene scene = GD.Load<PackedScene>(SceneHelper.GetScenePath("screens/card_library/library_pool_toggle"));
            NCardPoolFilter filter = scene?.Instantiate<NCardPoolFilter>(PackedScene.GenEditState.Disabled);
            if (filter == null)
            {
                Log.Warn("Guardian card library filter: failed to instantiate library_pool_toggle.");
                return null;
            }

            filter.Name = "GuardianPool";
            filter.Visible = true;
            filter.CustomMinimumSize = template.CustomMinimumSize;
            filter.FocusMode = template.FocusMode;
            parent.AddChild(filter);
            parent.MoveChild(filter, template.GetIndex() + 1);
            filter.Loc = new LocString("card_library", "POOL_GUARDIAN_TIP");

            ApplyGuardianPortrait(filter);

            filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(selected =>
            {
                var updateCardPoolFilter = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");
                updateCardPoolFilter?.Invoke(library, new object[] { selected });
            }));
            filter.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
            {
                AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl")?.SetValue(library, filter);
            }));
            Log.Info($"Guardian card library filter created under {parent.GetPath()} at index {filter.GetIndex()}.");
            return filter;
        }

        public static NCardPoolFilter FindOrCreateIroneyePoolFilter(
            NCardLibrary library,
            NCardPoolFilter template)
        {
            Node parent = template.GetParent();
            if (parent == null)
            {
                Log.Warn("Ironeye card library filter: template filter has no parent.");
                return null;
            }

            NCardPoolFilter existing =
                parent.GetNodeOrNull<NCardPoolFilter>("IroneyePool");
            if (existing != null)
            {
                existing.Visible = true;
                ApplyIroneyePortrait(existing);
                return existing;
            }

            PackedScene scene = GD.Load<PackedScene>(
                SceneHelper.GetScenePath("screens/card_library/library_pool_toggle"));
            NCardPoolFilter filter =
                scene?.Instantiate<NCardPoolFilter>(PackedScene.GenEditState.Disabled);
            if (filter == null)
            {
                Log.Warn("Ironeye card library filter: failed to instantiate library_pool_toggle.");
                return null;
            }

            filter.Name = "IroneyePool";
            filter.Visible = true;
            filter.CustomMinimumSize = template.CustomMinimumSize;
            filter.FocusMode = template.FocusMode;
            parent.AddChild(filter);
            parent.MoveChild(filter, Math.Min(template.GetIndex() + 2, parent.GetChildCount() - 1));
            filter.Loc = new LocString("card_library", "POOL_IRONEYE_TIP");
            ApplyIroneyePortrait(filter);

            filter.Connect(
                NCardPoolFilter.SignalName.Toggled,
                Callable.From<NCardPoolFilter>(selected =>
                {
                    var updateCardPoolFilter =
                        AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");
                    updateCardPoolFilter?.Invoke(library, new object[] { selected });
                }));
            filter.Connect(
                Control.SignalName.FocusEntered,
                Callable.From(() =>
                {
                    AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl")
                        ?.SetValue(library, filter);
                }));
            Log.Info(
                $"Ironeye card library filter created under {parent.GetPath()} at index {filter.GetIndex()}.");
            return filter;
        }

        public static NCardPoolFilter FindOrCreateRevenantPoolFilter(
            NCardLibrary library,
            NCardPoolFilter template)
        {
            Node parent = template.GetParent();
            if (parent == null)
            {
                Log.Warn("Revenant card library filter: template filter has no parent.");
                return null;
            }

            NCardPoolFilter existing = parent.GetNodeOrNull<NCardPoolFilter>("RevenantPool");
            if (existing != null)
            {
                existing.Visible = true;
                ApplyRevenantPortrait(existing);
                return existing;
            }

            PackedScene scene = GD.Load<PackedScene>(
                SceneHelper.GetScenePath("screens/card_library/library_pool_toggle"));
            NCardPoolFilter filter =
                scene?.Instantiate<NCardPoolFilter>(PackedScene.GenEditState.Disabled);
            if (filter == null)
            {
                Log.Warn("Revenant card library filter: failed to instantiate library_pool_toggle.");
                return null;
            }

            filter.Name = "RevenantPool";
            filter.Visible = true;
            filter.CustomMinimumSize = template.CustomMinimumSize;
            filter.FocusMode = template.FocusMode;
            parent.AddChild(filter);
            parent.MoveChild(filter, Math.Min(template.GetIndex() + 3, parent.GetChildCount() - 1));
            filter.Loc = new LocString("card_library", "POOL_REVENANT_TIP");
            ApplyRevenantPortrait(filter);

            filter.Connect(
                NCardPoolFilter.SignalName.Toggled,
                Callable.From<NCardPoolFilter>(selected =>
                {
                    var updateCardPoolFilter =
                        AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");
                    updateCardPoolFilter?.Invoke(library, new object[] { selected });
                }));
            filter.Connect(
                Control.SignalName.FocusEntered,
                Callable.From(() =>
                {
                    AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl")
                        ?.SetValue(library, filter);
                }));
            Log.Info(
                $"Revenant card library filter created under {parent.GetPath()} at index {filter.GetIndex()}.");
            return filter;
        }

        private static void ApplyGuardianPortrait(NCardPoolFilter filter)
        {
            Texture2D texture = PreloadManager.Cache.GetTexture2D("res://guardian_assets/character_icon_guardian.png");
            TextureRect image = filter.GetNodeOrNull<TextureRect>("Image");
            if (image != null)
            {
                image.Texture = texture;
                TextureRect shadow = image.GetNodeOrNull<TextureRect>("Shadow");
                if (shadow != null)
                {
                    shadow.Texture = texture;
                }

                image.Modulate = Colors.White;
                if (image.Material is ShaderMaterial shader)
                {
                    shader.SetShaderParameter("h", 1f);
                    shader.SetShaderParameter("s", 1f);
                    shader.SetShaderParameter("v", 1f);
                }
            }
        }

        private static void ApplyIroneyePortrait(NCardPoolFilter filter)
        {
            Texture2D texture = PreloadManager.Cache.GetTexture2D(
                "res://ironeye_assets/character_icon_ironeye.png");
            TextureRect image = filter.GetNodeOrNull<TextureRect>("Image");
            if (image == null)
                return;

            image.Texture = texture;
            TextureRect shadow = image.GetNodeOrNull<TextureRect>("Shadow");
            if (shadow != null)
                shadow.Texture = texture;

            image.Modulate = Colors.White;
            if (image.Material is ShaderMaterial shader)
            {
                shader.SetShaderParameter("h", 1f);
                shader.SetShaderParameter("s", 1f);
                shader.SetShaderParameter("v", 1f);
            }
        }

        private static void ApplyRevenantPortrait(NCardPoolFilter filter)
        {
            Texture2D texture = PreloadManager.Cache.GetTexture2D(
                "res://revenant_assets/character_icon_revenant.png");
            TextureRect image = filter.GetNodeOrNull<TextureRect>("Image");
            if (image == null)
                return;

            image.Texture = texture;
            TextureRect shadow = image.GetNodeOrNull<TextureRect>("Shadow");
            if (shadow != null)
                shadow.Texture = texture;

            image.Modulate = Colors.White;
            if (image.Material is ShaderMaterial shader)
            {
                shader.SetShaderParameter("h", 1f);
                shader.SetShaderParameter("s", 1f);
                shader.SetShaderParameter("v", 1f);
            }
        }
    }

    internal static class GuardianProgressHelper
    {
        public static void EnsureGuardianProgress(ProgressState progress)
        {
            CharacterModel[] modCharacters =
            {
                ModelDb.Character<Guardian>(),
                ModelDb.Character<Ironeye>(),
                ModelDb.Character<Revenant>(),
            };
            foreach (CharacterModel character in modCharacters)
            {
                progress.GetOrCreateCharacterStats(character.Id);
                foreach (CardModel card in character.CardPool.AllCards
                             .Concat(character.StartingDeck)
                             .Distinct())
                {
                    progress.MarkCardAsSeen(card.Id);
                }
            }
        }
    }
}
