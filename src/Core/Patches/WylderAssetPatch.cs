using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches
{
    [HarmonyPatch]
    public static class WylderAssetPatch
    {
        private const string CombatVisualsPath =
            "res://wylder_assets/combat_rig/wylder_combat_visuals.tscn";
        private const string RestSiteLayoutPath =
            "res://wylder_assets/rest_site/wylder_rest_site.tscn";
        private const string MerchantLayoutPath =
            "res://wylder_assets/merchant/wylder_merchant.tscn";
        private const string MultiplayerHandsPath =
            "res://wylder_assets/multiplayer_hands";
        private const string WylderTrailPath =
            "res://wylder_assets/card_trail_wylder.tscn";

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectBg(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = "res://wylder_assets/char_select_bg_wylder.tscn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectTransitionPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = "res://materials/transitions/wylder_transition_mat.tres";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EnergyCounterPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MerchantAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void RestSiteAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void TrailPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = WylderTrailPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AttackSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = "event:/sfx/ui/cards/card_impact_into_single";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CastSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = "event:/sfx/ui/cards/card_exhaust";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void DeathSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Wylder)
                __result = "event:/sfx/ui/combat/end_turn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Wylder)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://wylder_assets/character_icon_wylder.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconOutlineTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Wylder)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://wylder_assets/character_icon_wylder_outline.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPointingTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadWylderHand(__instance, "point", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmRockTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadWylderHand(__instance, "rock", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPaperTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadWylderHand(__instance, "paper", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmScissorsTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadWylderHand(__instance, "scissors", ref __result);
        }

        private static bool TryLoadWylderHand(
            CharacterModel character,
            string gesture,
            ref Texture2D texture)
        {
            if (character is not Wylder)
                return true;

            texture = PreloadManager.Cache.GetTexture2D(
                $"{MultiplayerHandsPath}/multiplayer_hand_wylder_{gesture}.png");
            return false;
        }

        [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
        [HarmonyPostfix]
        public static void WylderEnergyCounterTextures(Player player, ref NEnergyCounter __result)
        {
            if (player?.Character is not Wylder || __result == null)
                return;

            Control layers = __result.GetNode<Control>("%Layers");
            layers.GetNode<TextureRect>("Layer1").Texture =
                PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/wylder_orb_layer_1.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer2").Texture =
                PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/wylder_orb_layer_2.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer3").Texture =
                PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/wylder_orb_layer_3.png");
            layers.GetNode<TextureRect>("Layer4").Texture =
                PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/wylder_orb_layer_4.png");
            layers.GetNode<TextureRect>("Layer5").Texture =
                PreloadManager.Cache.GetTexture2D("res://wylder_assets/energy_counter/wylder_orb_layer_5.png");

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
        public static bool CreateWylderCardTrail(
            Control card,
            string characterTrailPath,
            ref NCardTrailVfx __result)
        {
            if (characterTrailPath != WylderTrailPath)
                return true;

            __result = PreloadManager.Cache
                .GetScene(SceneHelper.GetScenePath("vfx/card_trail_ironclad"))
                .Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
            AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow")
                ?.SetValue(__result, card);
            ApplyWylderTrailVisuals(__result);
            return false;
        }

        private static void ApplyWylderTrailVisuals(NCardTrailVfx trail)
        {
            Line2D outer = trail.GetNodeOrNull<Line2D>("Trails/OuterTrail");
            Line2D inner = trail.GetNodeOrNull<Line2D>("Trails/InnerTrail");
            if (outer != null)
            {
                outer.Modulate = new Color("52648DD2");
                outer.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("11182700"),
                        new Color("52648DB8"),
                        new Color("CBD5EAFF"),
                    },
                };
            }
            if (inner != null)
            {
                inner.Modulate = new Color("8694B4CC");
                inner.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("252E4A00"),
                        new Color("8694B4CF"),
                        new Color("E5EBF8FF"),
                    },
                };
            }

            Node2D sprites = trail.GetNodeOrNull<Node2D>("Sprites");
            if (sprites == null)
                return;

            Texture2D emblem = PreloadManager.Cache.GetTexture2D(
                "res://wylder_assets/energy_icon/wylder_energy_card_icon.png");
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
                    sprite.Modulate = new Color("CBD5EACC");
                    sprite.Scale = Vector2.One * 0.68f;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MapMarker(CharacterModel __instance, ref CompressedTexture2D __result)
        {
            if (__instance is Wylder)
            {
                __result = PreloadManager.Cache.GetCompressedTexture2D(
                    "res://wylder_assets/map_marker_wylder.png");
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
        [HarmonyPrefix]
        public static bool CreateVisuals(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (__instance is not Wylder)
                return true;

            try
            {
                Node2D layout = PreloadManager.Cache.GetScene(CombatVisualsPath)
                    .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
                __result = BuildCreatureVisuals(layout);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Wylder's independent combat rig failed to load.", exception);
            }

            return false;
        }

        private static NCreatureVisuals BuildCreatureVisuals(Node2D layout)
        {
            var visuals = new NCreatureVisuals { Name = "WylderCombatVisuals" };
            while (layout.GetChildCount() > 0)
            {
                Node child = layout.GetChild(0);
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
        public static void AssetPathsCharacterSelect(
            CharacterModel __instance,
            ref IEnumerable<string> __result)
        {
            if (__instance is not Wylder)
                return;

            __result = new[]
            {
                "res://wylder_assets/char_select_bg_wylder.tscn",
                "res://wylder_assets/character_select_wylder_bg.png",
                "res://wylder_assets/char_select_wylder.png",
                "res://wylder_assets/char_select_wylder_locked.png",
                "res://wylder_assets/character_icon_wylder.png",
                "res://materials/transitions/wylder_transition_mat.tres",
            };
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPaths), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AssetPaths(CharacterModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is not Wylder)
                return;

            __result = new[]
            {
                CombatVisualsPath,
                "res://wylder_assets/combat_rig/wylder_combat_character.png",
                "res://wylder_assets/combat_rig/wylder_pursuit_idle.png",
                "res://wylder_assets/combat_rig/wylder_attack.png",
                "res://wylder_assets/combat_rig/wylder_pursuit_attack.png",
                "res://wylder_assets/combat_rig/wylder_hit.png",
                "res://wylder_assets/combat_rig/wylder_block.png",
                "res://wylder_assets/combat_rig/wylder_death.png",
                "res://wylder_assets/combat_rig/wylder_combat_rig.gd",
                "res://wylder_assets/combat_rig/wylder_combat_rig.tscn",
                "res://wylder_assets/character_icon_wylder.png",
                "res://wylder_assets/character_icon_wylder_outline.png",
                "res://wylder_assets/character_icon_wylder.tscn",
                "res://wylder_assets/energy_counter/wylder_orb_layer_1.png",
                "res://wylder_assets/energy_counter/wylder_orb_layer_2.png",
                "res://wylder_assets/energy_counter/wylder_orb_layer_3.png",
                "res://wylder_assets/energy_counter/wylder_orb_layer_4.png",
                "res://wylder_assets/energy_counter/wylder_orb_layer_5.png",
                "res://wylder_assets/energy_icon/wylder_energy_card_icon.png",
                "res://images/atlases/ui_atlas.sprites/card/energy_wylder.tres",
                "res://images/packed/sprite_fonts/wylder_energy_icon.png",
                "res://wylder_assets/relics/wylder_single_earring.png",
                RestSiteLayoutPath,
                "res://wylder_assets/rest_site/wylder_rest_site.png",
                SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
                MerchantLayoutPath,
                "res://wylder_assets/merchant/wylder_merchant.png",
                SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
                "res://materials/transitions/wylder_transition_mat.tres",
                "res://wylder_assets/map_marker_wylder.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_wylder_point.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_wylder_rock.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_wylder_paper.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_wylder_scissors.png",
                "res://wylder_assets/card_trail_wylder.tscn",
            };
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
        [HarmonyPostfix]
        public static void RestSiteVisuals(Player player, ref NRestSiteCharacter __result)
        {
            if (player?.Character is not Wylder || __result == null)
                return;

            if (__result.HasMeta("wylder_rest_site"))
            {
                __result.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
                return;
            }

            Node2D layout = PreloadManager.Cache.GetScene(RestSiteLayoutPath)
                .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
            foreach (Node child in __result.GetChildren())
            {
                if (child.GetClass() != "SpineSprite")
                    continue;

                __result.RemoveChild(child);
                child.QueueFree();
            }

            layout.Name = "WylderRestVisual";
            __result.AddChild(layout);
            SetOwnerRecursively(layout, __result);
            __result.SetMeta("wylder_rest_site", true);
            layout.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
        [HarmonyPostfix]
        public static void FlipRestSiteVisual(NRestSiteCharacter __instance)
        {
            if (!__instance.HasMeta("wylder_rest_site"))
                return;

            Node2D visual = __instance.GetNodeOrNull<Node2D>("WylderRestVisual");
            if (visual == null)
                return;

            visual.Scale = new Vector2(-visual.Scale.X, visual.Scale.Y);
            visual.Position = new Vector2(-visual.Position.X, visual.Position.Y);
        }

        [HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
        [HarmonyPostfix]
        public static void MerchantVisuals(NMerchantRoom __instance, List<Player> ____players)
        {
            IReadOnlyList<NMerchantCharacter> visuals = __instance.PlayerVisuals;
            int count = Math.Min(visuals.Count, ____players.Count);
            for (int i = 0; i < count; i++)
            {
                if (____players[i].Character is Wylder)
                    ReplaceMerchantVisual(visuals[i]);
            }
        }

        private static void ReplaceMerchantVisual(NMerchantCharacter merchantCharacter)
        {
            if (merchantCharacter.HasMeta("wylder_merchant"))
                return;

            Node2D layout = PreloadManager.Cache.GetScene(MerchantLayoutPath)
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
            merchantCharacter.SetMeta("wylder_merchant", true);
            merchantCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
        [HarmonyPrefix]
        public static bool MerchantReady(NMerchantCharacter __instance)
        {
            if (!__instance.HasMeta("wylder_merchant"))
                return true;

            __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
            return false;
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
        [HarmonyPrefix]
        public static bool MerchantAnimation(NMerchantCharacter __instance, string anim)
        {
            if (!__instance.HasMeta("wylder_merchant"))
                return true;

            AnimationPlayer player =
                __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            player?.Play(anim == "die" ? "die" : "relaxed_loop");
            return false;
        }
    }
}
