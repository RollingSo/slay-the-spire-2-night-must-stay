using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using sts2mod.Core.Models.Characters;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch]
    public static class IroneyeAssetPatch
    {
        private const string CombatVisualsPath =
            "res://ironeye_assets/combat_rig/ironeye_combat_visuals.tscn";
        private const string RestSiteLayoutPath =
            "res://ironeye_assets/rest_site/ironeye_rest_site.tscn";
        private const string MerchantLayoutPath =
            "res://ironeye_assets/merchant/ironeye_merchant.tscn";
        private const string MultiplayerHandsPath =
            "res://ironeye_assets/multiplayer_hands";
        private const string IroneyeTrailPath =
            "res://ironeye_assets/card_trail_ironeye.tscn";

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectBg(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = "res://ironeye_assets/char_select_bg_ironeye.tscn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectTransitionPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = "res://materials/transitions/ironeye_transition_mat.tres";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EnergyCounterPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MerchantAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void RestSiteAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void TrailPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = IroneyeTrailPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AttackSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = "event:/sfx/ui/cards/card_impact_into_single";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CastSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = "event:/sfx/ui/cards/card_exhaust";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void DeathSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Ironeye)
                __result = "event:/sfx/ui/combat/end_turn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Ironeye)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://ironeye_assets/character_icon_ironeye.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconOutlineTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Ironeye)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://ironeye_assets/character_icon_ironeye_outline.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPointingTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadIroneyeHand(__instance, "point", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmRockTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadIroneyeHand(__instance, "rock", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPaperTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadIroneyeHand(__instance, "paper", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmScissorsTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadIroneyeHand(__instance, "scissors", ref __result);
        }

        private static bool TryLoadIroneyeHand(
            CharacterModel character,
            string gesture,
            ref Texture2D texture)
        {
            if (character is not Ironeye)
                return true;

            texture = PreloadManager.Cache.GetTexture2D(
                $"{MultiplayerHandsPath}/multiplayer_hand_ironeye_{gesture}.png");
            return false;
        }

        [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
        [HarmonyPostfix]
        public static void IroneyeEnergyCounterTextures(Player player, ref NEnergyCounter __result)
        {
            if (player?.Character is not Ironeye || __result == null)
                return;

            Control layers = __result.GetNode<Control>("%Layers");
            layers.GetNode<TextureRect>("Layer1").Texture =
                PreloadManager.Cache.GetTexture2D("res://ironeye_assets/energy_counter/ironeye_orb_layer_1.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer2").Texture =
                PreloadManager.Cache.GetTexture2D("res://ironeye_assets/energy_counter/ironeye_orb_layer_2.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer3").Texture =
                PreloadManager.Cache.GetTexture2D("res://ironeye_assets/energy_counter/ironeye_orb_layer_3.png");
            layers.GetNode<TextureRect>("Layer4").Texture =
                PreloadManager.Cache.GetTexture2D("res://ironeye_assets/energy_counter/ironeye_orb_layer_4.png");
            layers.GetNode<TextureRect>("Layer5").Texture =
                PreloadManager.Cache.GetTexture2D("res://ironeye_assets/energy_counter/ironeye_orb_layer_5.png");

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
        public static bool CreateIroneyeCardTrail(
            Control card,
            string characterTrailPath,
            ref NCardTrailVfx __result)
        {
            if (characterTrailPath != IroneyeTrailPath)
                return true;

            __result = PreloadManager.Cache
                .GetScene(SceneHelper.GetScenePath("vfx/card_trail_ironclad"))
                .Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
            AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow")
                ?.SetValue(__result, card);
            ApplyIroneyeTrailVisuals(__result);
            return false;
        }

        private static void ApplyIroneyeTrailVisuals(NCardTrailVfx trail)
        {
            Line2D outer = trail.GetNodeOrNull<Line2D>("Trails/OuterTrail");
            Line2D inner = trail.GetNodeOrNull<Line2D>("Trails/InnerTrail");
            if (outer != null)
            {
                outer.Modulate = new Color("728A33D2");
                outer.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("18210B00"),
                        new Color("859B35B8"),
                        new Color("EAF56FFF"),
                    },
                };
            }
            if (inner != null)
            {
                inner.Modulate = new Color("D7E85FCC");
                inner.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("71852B00"),
                        new Color("C8DF51CF"),
                        new Color("FFFBC0FF"),
                    },
                };
            }

            Node2D sprites = trail.GetNodeOrNull<Node2D>("Sprites");
            if (sprites == null)
                return;

            Texture2D emblem = PreloadManager.Cache.GetTexture2D(
                "res://ironeye_assets/energy_icon/ironeye_energy_card_icon.png");
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
                    sprite.Modulate = new Color("D7EA59CC");
                    sprite.Scale = Vector2.One * 0.68f;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MapMarker(CharacterModel __instance, ref CompressedTexture2D __result)
        {
            if (__instance is Ironeye)
            {
                __result = PreloadManager.Cache.GetCompressedTexture2D(
                    "res://ironeye_assets/map_marker_ironeye.png");
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
        [HarmonyPrefix]
        public static bool CreateVisuals(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (__instance is not Ironeye)
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
                    $"Ironeye's independent combat rig failed to load.", exception);
            }

            return false;
        }

        private static NCreatureVisuals BuildCreatureVisuals(Node2D layout)
        {
            var visuals = new NCreatureVisuals { Name = "IroneyeCombatVisuals" };
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
            if (__instance is not Ironeye)
                return;

            __result = new[]
            {
                "res://ironeye_assets/char_select_bg_ironeye.tscn",
                "res://ironeye_assets/character_select_ironeye_bg.png",
                "res://ironeye_assets/char_select_ironeye.png",
                "res://ironeye_assets/char_select_ironeye_locked.png",
                "res://ironeye_assets/character_icon_ironeye.png",
                "res://materials/transitions/ironeye_transition_mat.tres",
            };
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPaths), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AssetPaths(CharacterModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is not Ironeye)
                return;

            __result = new[]
            {
                CombatVisualsPath,
                "res://ironeye_assets/combat_rig/ironeye_combat_character.png",
                "res://ironeye_assets/combat_rig/ironeye_attack.png",
                "res://ironeye_assets/combat_rig/ironeye_hit.png",
                "res://ironeye_assets/combat_rig/ironeye_combat_rig.gd",
                "res://ironeye_assets/combat_rig/ironeye_combat_rig.tscn",
                "res://ironeye_assets/character_icon_ironeye.png",
                "res://ironeye_assets/character_icon_ironeye_outline.png",
                "res://ironeye_assets/character_icon_ironeye.tscn",
                "res://ironeye_assets/energy_counter/ironeye_orb_layer_1.png",
                "res://ironeye_assets/energy_counter/ironeye_orb_layer_2.png",
                "res://ironeye_assets/energy_counter/ironeye_orb_layer_3.png",
                "res://ironeye_assets/energy_counter/ironeye_orb_layer_4.png",
                "res://ironeye_assets/energy_counter/ironeye_orb_layer_5.png",
                "res://ironeye_assets/energy_icon/ironeye_energy_card_icon.png",
                "res://images/atlases/ui_atlas.sprites/card/energy_ironeye.tres",
                "res://images/packed/sprite_fonts/ironeye_energy_icon.png",
                "res://ironeye_assets/relics/cursemark_signet.png",
                RestSiteLayoutPath,
                "res://ironeye_assets/rest_site/ironeye_rest_site.png",
                SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
                MerchantLayoutPath,
                "res://ironeye_assets/merchant/ironeye_merchant.png",
                SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
                "res://materials/transitions/ironeye_transition_mat.tres",
                "res://ironeye_assets/map_marker_ironeye.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_ironeye_point.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_ironeye_rock.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_ironeye_paper.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_ironeye_scissors.png",
                "res://ironeye_assets/card_trail_ironeye.tscn",
            };
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
        [HarmonyPostfix]
        public static void RestSiteVisuals(Player player, ref NRestSiteCharacter __result)
        {
            if (player?.Character is not Ironeye || __result == null)
                return;

            if (__result.HasMeta("ironeye_rest_site"))
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

            layout.Name = "IroneyeRestVisual";
            __result.AddChild(layout);
            SetOwnerRecursively(layout, __result);
            __result.SetMeta("ironeye_rest_site", true);
            layout.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
        [HarmonyPostfix]
        public static void FlipRestSiteVisual(NRestSiteCharacter __instance)
        {
            if (!__instance.HasMeta("ironeye_rest_site"))
                return;

            Node2D visual = __instance.GetNodeOrNull<Node2D>("IroneyeRestVisual");
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
                if (____players[i].Character is Ironeye)
                    ReplaceMerchantVisual(visuals[i]);
            }
        }

        private static void ReplaceMerchantVisual(NMerchantCharacter merchantCharacter)
        {
            if (merchantCharacter.HasMeta("ironeye_merchant"))
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
            merchantCharacter.SetMeta("ironeye_merchant", true);
            merchantCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
        [HarmonyPrefix]
        public static bool MerchantReady(NMerchantCharacter __instance)
        {
            if (!__instance.HasMeta("ironeye_merchant"))
                return true;

            __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
            return false;
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
        [HarmonyPrefix]
        public static bool MerchantAnimation(NMerchantCharacter __instance, string anim)
        {
            if (!__instance.HasMeta("ironeye_merchant"))
                return true;

            AnimationPlayer player =
                __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            player?.Play(anim == "die" ? "die" : "relaxed_loop");
            return false;
        }
    }
}
