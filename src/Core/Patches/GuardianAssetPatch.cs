using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch]
    public static class GuardianAssetPatch
    {
        private const string GuardianCombatVisualsPath = "res://guardian_assets/combat_rig/guardian_combat_visuals.tscn";
        private const string GuardianEnergyCounterPath = "res://guardian_assets/energy_counter/guardian_energy_counter.tscn";
        private const string GuardianMerchantLayoutPath = "res://guardian_assets/merchant/guardian_merchant.tscn";
        private const string GuardianRestSiteLayoutPath = "res://guardian_assets/rest_site/guardian_rest_site.tscn";
        private const string GuardianTransitionPath = "res://materials/transitions/guardian_transition_mat.tres";
        private const string GuardianTrailPath = "res://guardian_assets/card_trail_guardian.tscn";
        private const string GuardianMultiplayerHandsPath = "res://guardian_assets/multiplayer_hands";

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectBg(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = "res://guardian_assets/char_select_bg_guardian.tscn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectTransitionPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = GuardianTransitionPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EnergyCounterPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = GuardianEnergyCounterPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MerchantAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void RestSiteAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void TrailPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = GuardianTrailPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AttackSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = "event:/sfx/ui/cards/card_impact_into_single";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CastSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = "event:/sfx/ui/cards/card_exhaust";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void DeathSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Guardian)
                __result = "event:/sfx/ui/combat/end_turn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is Guardian)
            {
                __result = PreloadManager.Cache.GetTexture2D("res://guardian_assets/character_icon_guardian.png");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconOutlineTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is Guardian)
            {
                __result = PreloadManager.Cache.GetTexture2D("res://guardian_assets/character_icon_guardian_outline.png");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPointingTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadGuardianHand(__instance, "point", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmRockTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadGuardianHand(__instance, "rock", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPaperTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadGuardianHand(__instance, "paper", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmScissorsTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadGuardianHand(__instance, "scissors", ref __result);
        }

        private static bool TryLoadGuardianHand(CharacterModel character, string gesture, ref Texture2D texture)
        {
            if (character is not Guardian)
                return true;

            texture = PreloadManager.Cache.GetTexture2D(
                $"{GuardianMultiplayerHandsPath}/multiplayer_hand_guardian_{gesture}.png");
            return false;
        }

        [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
        [HarmonyPostfix]
        public static void GuardianEnergyCounterTextures(Player player, ref NEnergyCounter __result)
        {
            if (player?.Character is not Guardian || __result == null)
                return;

            Control layers = __result.GetNode<Control>("%Layers");
            layers.GetNode<TextureRect>("Layer1").Texture =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/energy_counter/guardian_orb_layer_1.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer2").Texture =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/energy_counter/guardian_orb_layer_2.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer3").Texture =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/energy_counter/guardian_orb_layer_3.png");
            layers.GetNode<TextureRect>("Layer4").Texture =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/energy_counter/guardian_orb_layer_4.png");
            layers.GetNode<TextureRect>("Layer5").Texture =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/energy_counter/guardian_orb_layer_5.png");

            // The original scene supplies the engine-owned NEnergyCounter type.
            // Its character-specific particles are disabled so only Guardian
            // artwork is visible.
            DisableCharacterVfx(__result.GetNodeOrNull<CanvasItem>("%EnergyVfxBack"));
            DisableCharacterVfx(__result.GetNodeOrNull<CanvasItem>("%EnergyVfxFront"));
        }

        private static void DisableCharacterVfx(CanvasItem vfx)
        {
            if (vfx == null)
                return;

            vfx.Visible = false;
            vfx.ProcessMode = Node.ProcessModeEnum.Disabled;
        }

        [HarmonyPatch(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create))]
        [HarmonyPrefix]
        public static bool CreateGuardianCardTrail(
            Control card,
            string characterTrailPath,
            ref NCardTrailVfx __result)
        {
            if (characterTrailPath != GuardianTrailPath)
                return true;

            __result = PreloadManager.Cache
                .GetScene(SceneHelper.GetScenePath("vfx/card_trail_ironclad"))
                .Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
            AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow")
                ?.SetValue(__result, card);
            ApplyGuardianTrailVisuals(__result);
            return false;
        }

        private static void ApplyGuardianTrailVisuals(NCardTrailVfx trail)
        {
            Line2D outer = trail.GetNodeOrNull<Line2D>("Trails/OuterTrail");
            Line2D inner = trail.GetNodeOrNull<Line2D>("Trails/InnerTrail");
            if (outer != null)
            {
                outer.Modulate = new Color("1F86D1D2");
                outer.Gradient = new Gradient
                {
                    Colors = new[] {
                        new Color("061E3A00"),
                        new Color("248BD6B8"),
                        new Color("B8EEFFFF")
                    }
                };
            }
            if (inner != null)
            {
                inner.Modulate = new Color("9DEBFFCC");
                inner.Gradient = new Gradient
                {
                    Colors = new[] {
                        new Color("2A8BCB00"),
                        new Color("7DDEFFCF"),
                        new Color("F0FCFFFF")
                    }
                };
            }

            Node2D sprites = trail.GetNodeOrNull<Node2D>("Sprites");
            if (sprites == null)
                return;

            Texture2D emblem =
                PreloadManager.Cache.GetTexture2D("res://guardian_assets/guardian_trail_emblem.png");
            foreach (Node child in sprites.GetChildren())
            {
                if (child is CpuParticles2D particles)
                {
                    particles.Emitting = false;
                    particles.Visible = false;
                }
                else if (child is Sprite2D sprite)
                {
                    sprite.Texture = emblem;
                    sprite.Modulate = new Color("8FE8FFCC");
                    sprite.Scale = Vector2.One * 0.68f;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MapMarker(CharacterModel __instance, ref CompressedTexture2D __result)
        {
            if (__instance is Guardian)
                __result = PreloadManager.Cache.GetCompressedTexture2D("res://guardian_assets/map_marker_guardian.png");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
        [HarmonyPrefix]
        public static bool CreateVisuals(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (__instance is not Guardian)
                return true;

            try
            {
                var layout = PreloadManager.Cache.GetScene(GuardianCombatVisualsPath)
                    .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
                __result = BuildCreatureVisuals(layout);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Guardian combat rig failed to load. Refusing to substitute another character's visuals.",
                    exception);
            }
            return false;
        }

        private static NCreatureVisuals BuildCreatureVisuals(Node2D layout)
        {
            var visuals = new NCreatureVisuals { Name = "GuardianCombatVisuals" };
            while (layout.GetChildCount() > 0)
            {
                var child = layout.GetChild(0);
                layout.RemoveChild(child);
                visuals.AddChild(child);
                SetOwnerRecursively(child, visuals);
            }

            layout.Free();
            return visuals;
        }

        private static void SetOwnerRecursively(Node node, Node owner)
        {
            node.Owner = owner;
            foreach (Node child in node.GetChildren())
                SetOwnerRecursively(child, owner);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPathsCharacterSelect), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AssetPathsCharacterSelect(CharacterModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is Guardian)
            {
                __result = new[]
                {
                    "res://guardian_assets/char_select_bg_guardian.tscn",
                    "res://guardian_assets/char_select_guardian.png",
                    "res://guardian_assets/character_icon_guardian.png",
                    "res://guardian_assets/char_select_guardian_locked.png",
                    GuardianTransitionPath
                };
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPaths), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AssetPaths(CharacterModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is Guardian)
            {
                __result = new[]
                {
                    GuardianCombatVisualsPath,
                    "res://guardian_assets/character_icon_guardian.png",
                    "res://guardian_assets/character_icon_guardian_outline.png",
                    "res://guardian_assets/character_icon_guardian.tscn",
                    GuardianEnergyCounterPath,
                    SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter"),
                    "res://guardian_assets/energy_counter/guardian_orb_layer_1.png",
                    "res://guardian_assets/energy_counter/guardian_orb_layer_2.png",
                    "res://guardian_assets/energy_counter/guardian_orb_layer_3.png",
                    "res://guardian_assets/energy_counter/guardian_orb_layer_4.png",
                    "res://guardian_assets/energy_counter/guardian_orb_layer_5.png",
                    "res://guardian_assets/energy_icon/guardian_energy_card_icon.png",
                    "res://images/atlases/ui_atlas.sprites/card/energy_guardian.tres",
                    "res://images/packed/sprite_fonts/guardian_energy_icon.png",
                    "res://guardian_assets/relics/single_wing_greatshield.png",
                    "res://guardian_assets/relics/twin_wing_greatshield.png",
                    GuardianRestSiteLayoutPath,
                    SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
                    GuardianMerchantLayoutPath,
                    SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
                    GuardianTransitionPath,
                    "res://guardian_assets/guardian_transition_mask.png",
                    "res://guardian_assets/map_marker_guardian.png",
                    $"{GuardianMultiplayerHandsPath}/multiplayer_hand_guardian_point.png",
                    $"{GuardianMultiplayerHandsPath}/multiplayer_hand_guardian_rock.png",
                    $"{GuardianMultiplayerHandsPath}/multiplayer_hand_guardian_paper.png",
                    $"{GuardianMultiplayerHandsPath}/multiplayer_hand_guardian_scissors.png",
                    GuardianTrailPath,
                    SceneHelper.GetScenePath("vfx/card_trail_ironclad"),
                    "res://guardian_assets/guardian_trail_emblem.png"
                };
            }
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
        [HarmonyPostfix]
        public static void GuardianRestSiteVisuals(Player player, ref NRestSiteCharacter __result)
        {
            if (player?.Character is not Guardian || __result == null)
                return;

            if (__result.HasMeta("guardian_rest_site"))
            {
                __result.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
                return;
            }

            Node2D layout = PreloadManager.Cache.GetScene(GuardianRestSiteLayoutPath)
                .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);

            foreach (Node child in __result.GetChildren())
            {
                if (child.GetClass() != "SpineSprite")
                    continue;

                __result.RemoveChild(child);
                child.QueueFree();
            }

            layout.Name = "GuardianRestVisual";
            __result.AddChild(layout);
            SetOwnerRecursively(layout, __result);
            __result.SetMeta("guardian_rest_site", true);
            layout.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
        [HarmonyPostfix]
        public static void FlipGuardianRestSiteVisual(NRestSiteCharacter __instance)
        {
            if (!__instance.HasMeta("guardian_rest_site"))
                return;

            Node2D visual = __instance.GetNodeOrNull<Node2D>("GuardianRestVisual");
            if (visual == null)
                return;

            visual.Scale = new Vector2(-visual.Scale.X, visual.Scale.Y);
            visual.Position = new Vector2(-visual.Position.X, visual.Position.Y);
        }

        [HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
        [HarmonyPostfix]
        public static void GuardianMerchantVisuals(NMerchantRoom __instance, List<Player> ____players)
        {
            IReadOnlyList<NMerchantCharacter> visuals = __instance.PlayerVisuals;
            int count = Math.Min(visuals.Count, ____players.Count);
            for (int i = 0; i < count; i++)
            {
                if (____players[i].Character is Guardian)
                    ReplaceMerchantVisual(visuals[i]);
            }
        }

        private static void ReplaceMerchantVisual(NMerchantCharacter merchantCharacter)
        {
            if (merchantCharacter.HasMeta("guardian_merchant"))
                return;

            Node2D layout = PreloadManager.Cache.GetScene(GuardianMerchantLayoutPath)
                .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);

            while (merchantCharacter.GetChildCount() > 0)
            {
                Node child = merchantCharacter.GetChild(0);
                merchantCharacter.RemoveChild(child);
                child.QueueFree();
            }

            while (layout.GetChildCount() > 0)
            {
                Node child = layout.GetChild(0);
                layout.RemoveChild(child);
                merchantCharacter.AddChild(child);
                SetOwnerRecursively(child, merchantCharacter);
            }

            layout.Free();
            merchantCharacter.SetMeta("guardian_merchant", true);
            merchantCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("relaxed_loop");
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
        [HarmonyPrefix]
        public static bool GuardianMerchantReady(NMerchantCharacter __instance)
        {
            if (!__instance.HasMeta("guardian_merchant"))
                return true;

            __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("relaxed_loop");
            return false;
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
        [HarmonyPrefix]
        public static bool GuardianMerchantAnimation(NMerchantCharacter __instance, string anim)
        {
            if (!__instance.HasMeta("guardian_merchant"))
                return true;

            AnimationPlayer player = __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if (player != null)
                player.Play(anim == "die" ? "die" : "relaxed_loop");
            return false;
        }
    }
}
