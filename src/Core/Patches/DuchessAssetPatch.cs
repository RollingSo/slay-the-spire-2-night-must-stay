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
    public static class DuchessAssetPatch
    {
        private const string CombatVisualsPath =
            "res://duchess_assets/combat_rig/duchess_combat_visuals.tscn";
        private const string RestSiteLayoutPath =
            "res://duchess_assets/rest_site/duchess_rest_site.tscn";
        private const string MerchantLayoutPath =
            "res://duchess_assets/merchant/duchess_merchant.tscn";
        private const string MultiplayerHandsPath =
            "res://duchess_assets/multiplayer_hands";
        private const string DuchessTrailPath =
            "res://duchess_assets/card_trail_duchess.tscn";
        private const string MomentCounterMeta = "night_must_stay_duchess_moment_counter";
        private const string MomentCounterName = "DuchessMomentCounter";

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectBg(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = "res://duchess_assets/char_select_bg_duchess.tscn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterSelectTransitionPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = "res://materials/transitions/duchess_transition_mat.tres";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void EnergyCounterPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MerchantAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void RestSiteAnimPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site");
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
        [HarmonyPostfix]
        public static void TrailPath(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = DuchessTrailPath;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AttackSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = "event:/sfx/ui/cards/card_impact_into_single";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CastSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = "event:/sfx/ui/cards/card_exhaust";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
        [HarmonyPostfix]
        public static void DeathSfx(CharacterModel __instance, ref string __result)
        {
            if (__instance is Duchess)
                __result = "event:/sfx/ui/combat/end_turn";
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Duchess)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://duchess_assets/character_icon_duchess.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IconOutlineTexture(CharacterModel __instance, ref Texture2D __result)
        {
            if (__instance is not Duchess)
                return true;

            __result = PreloadManager.Cache.GetTexture2D(
                "res://duchess_assets/character_icon_duchess_outline.png");
            return false;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPointingTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadDuchessHand(__instance, "point", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmRockTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadDuchessHand(__instance, "rock", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmPaperTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadDuchessHand(__instance, "paper", ref __result);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmScissorsTexture(CharacterModel __instance, ref Texture2D __result)
        {
            return TryLoadDuchessHand(__instance, "scissors", ref __result);
        }

        private static bool TryLoadDuchessHand(
            CharacterModel character,
            string gesture,
            ref Texture2D texture)
        {
            if (character is not Duchess)
                return true;

            texture = PreloadManager.Cache.GetTexture2D(
                $"{MultiplayerHandsPath}/multiplayer_hand_duchess_{gesture}.png");
            return false;
        }

        [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
        [HarmonyPostfix]
        public static void DuchessEnergyCounterTextures(Player player, ref NEnergyCounter __result)
        {
            if (player?.Character is not Duchess || __result == null)
                return;

            Control layers = __result.GetNode<Control>("%Layers");
            layers.GetNode<TextureRect>("Layer1").Texture =
                PreloadManager.Cache.GetTexture2D("res://duchess_assets/energy_counter/duchess_orb_layer_1.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer2").Texture =
                PreloadManager.Cache.GetTexture2D("res://duchess_assets/energy_counter/duchess_orb_layer_2.png");
            layers.GetNode<TextureRect>("RotationLayers/Layer3").Texture =
                PreloadManager.Cache.GetTexture2D("res://duchess_assets/energy_counter/duchess_orb_layer_3.png");
            layers.GetNode<TextureRect>("Layer4").Texture =
                PreloadManager.Cache.GetTexture2D("res://duchess_assets/energy_counter/duchess_orb_layer_4.png");
            layers.GetNode<TextureRect>("Layer5").Texture =
                PreloadManager.Cache.GetTexture2D("res://duchess_assets/energy_counter/duchess_orb_layer_5.png");

            DisableCharacterVfx(__result.GetNodeOrNull<CanvasItem>("%EnergyVfxBack"));
            DisableCharacterVfx(__result.GetNodeOrNull<CanvasItem>("%EnergyVfxFront"));
        }

        [HarmonyPatch(typeof(NStarCounter), nameof(NStarCounter.Initialize))]
        [HarmonyPostfix]
        public static void InitializeDuchessMomentCounter(NStarCounter __instance, Player player)
        {
            if (player?.Character is not Duchess)
                return;

            if (__instance.HasMeta(MomentCounterMeta))
            {
                ApplyMomentCounterVisuals(__instance);
                __instance.Visible = true;
                return;
            }

            Node parent = __instance.GetParent();
            if (parent == null || parent.GetNodeOrNull<NStarCounter>(MomentCounterName) != null)
                return;

            if (__instance.Duplicate() is not NStarCounter momentCounter)
                return;

            momentCounter.Name = MomentCounterName;
            momentCounter.SetMeta(MomentCounterMeta, true);
            __instance.AddSibling(momentCounter);
            momentCounter.Initialize(player);
            momentCounter.Position = __instance.Position + new Vector2(88f, 0f);
            ApplyMomentCounterVisuals(momentCounter);
            momentCounter.Visible = true;
        }

        [HarmonyPatch(typeof(NStarCounter), "RefreshVisibility")]
        [HarmonyPostfix]
        public static void KeepDuchessMomentCounterVisible(NStarCounter __instance)
        {
            if (!__instance.HasMeta(MomentCounterMeta))
                return;
            ApplyMomentCounterVisuals(__instance);
            __instance.Visible = true;
        }

        [HarmonyPatch(typeof(NStarCounter), nameof(NStarCounter._Process))]
        [HarmonyPrefix]
        public static bool RefreshDuchessMomentValue(NStarCounter __instance)
        {
            if (!__instance.HasMeta(MomentCounterMeta))
                return true;
            Player player = AccessTools.Field(typeof(NStarCounter), "_player")?.GetValue(__instance) as Player;
            int moment = DuchessMomentPower.Current(player);
            int displayed = (int)(AccessTools.Field(typeof(NStarCounter), "_displayedStarCount")
                ?.GetValue(__instance) ?? -1);
            if (displayed != moment)
                AccessTools.Method(typeof(NStarCounter), "SetStarCountText")?.Invoke(__instance, new object[] { moment });
            return false;
        }

        private static void ApplyMomentCounterVisuals(NStarCounter counter)
        {
            Texture2D watch = PreloadManager.Cache.GetTexture2D(
                "res://duchess_assets/relics/duchess_old_pocketwatch.png");
            Control icon = AccessTools.Field(typeof(NStarCounter), "_icon")?.GetValue(counter) as Control;
            Control rotationLayers = AccessTools.Field(typeof(NStarCounter), "_rotationLayers")?.GetValue(counter) as Control;
            if (HoverTipFactory.FromPower<DuchessMomentDescriptionPower>() is HoverTip momentTip)
                AccessTools.Field(typeof(NStarCounter), "_hoverTip")?.SetValue(counter, momentTip);
            if (rotationLayers != null)
                rotationLayers.Visible = false;
            if (icon is TextureRect iconTexture)
                iconTexture.Texture = watch;
            if (icon == null)
                return;
            bool assigned = icon is TextureRect;
            foreach (Node node in icon.FindChildren("*", "TextureRect", true, false))
            {
                if (node is not TextureRect texture)
                    continue;
                if (!assigned)
                {
                    texture.Texture = watch;
                    texture.Visible = true;
                    assigned = true;
                }
                else
                {
                    texture.Visible = false;
                }
            }
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
        public static bool CreateDuchessCardTrail(
            Control card,
            string characterTrailPath,
            ref NCardTrailVfx __result)
        {
            if (characterTrailPath != DuchessTrailPath)
                return true;

            __result = PreloadManager.Cache
                .GetScene(SceneHelper.GetScenePath("vfx/card_trail_ironclad"))
                .Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
            AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow")
                ?.SetValue(__result, card);
            ApplyDuchessTrailVisuals(__result);
            return false;
        }

        private static void ApplyDuchessTrailVisuals(NCardTrailVfx trail)
        {
            Line2D outer = trail.GetNodeOrNull<Line2D>("Trails/OuterTrail");
            Line2D inner = trail.GetNodeOrNull<Line2D>("Trails/InnerTrail");
            if (outer != null)
            {
                outer.Modulate = new Color("6383BAD2");
                outer.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("15213700"),
                        new Color("6E93CFB8"),
                        new Color("D2E7FFFF"),
                    },
                };
            }
            if (inner != null)
            {
                inner.Modulate = new Color("B2D5FFCC");
                inner.Gradient = new Gradient
                {
                    Colors = new[]
                    {
                        new Color("617CB800"),
                        new Color("92C3FFCF"),
                        new Color("EBF5FFFF"),
                    },
                };
            }

            Node2D sprites = trail.GetNodeOrNull<Node2D>("Sprites");
            if (sprites == null)
                return;

            Texture2D emblem = PreloadManager.Cache.GetTexture2D(
                "res://duchess_assets/energy_icon/duchess_energy_card_icon.png");
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
                    sprite.Modulate = new Color("B9D9FFCC");
                    sprite.Scale = Vector2.One * 0.68f;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
        [HarmonyPostfix]
        public static void MapMarker(CharacterModel __instance, ref CompressedTexture2D __result)
        {
            if (__instance is Duchess)
            {
                __result = PreloadManager.Cache.GetCompressedTexture2D(
                    "res://duchess_assets/map_marker_duchess.png");
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
        [HarmonyPrefix]
        public static bool CreateVisuals(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (__instance is not Duchess)
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
                    $"Duchess's independent combat rig failed to load.", exception);
            }

            return false;
        }

        private static NCreatureVisuals BuildCreatureVisuals(Node2D layout)
        {
            var visuals = new NCreatureVisuals { Name = "DuchessCombatVisuals" };
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
            if (__instance is not Duchess)
                return;

            __result = new[]
            {
                "res://duchess_assets/char_select_bg_duchess.tscn",
                "res://duchess_assets/character_select_duchess_bg.png",
                "res://duchess_assets/char_select_duchess.png",
                "res://duchess_assets/char_select_duchess_locked.png",
                "res://duchess_assets/character_icon_duchess.png",
                "res://materials/transitions/duchess_transition_mat.tres",
            };
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPaths), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AssetPaths(CharacterModel __instance, ref IEnumerable<string> __result)
        {
            if (__instance is not Duchess)
                return;

            __result = new[]
            {
                CombatVisualsPath,
                "res://duchess_assets/combat_rig/duchess_combat_character.png",
                "res://duchess_assets/combat_rig/duchess_attack.png",
                "res://duchess_assets/combat_rig/duchess_hit.png",
                "res://duchess_assets/combat_rig/duchess_combat_rig.gd",
                "res://duchess_assets/combat_rig/duchess_combat_rig.tscn",
                "res://duchess_assets/character_icon_duchess.png",
                "res://duchess_assets/character_icon_duchess_outline.png",
                "res://duchess_assets/character_icon_duchess.tscn",
                "res://duchess_assets/energy_counter/duchess_orb_layer_1.png",
                "res://duchess_assets/energy_counter/duchess_orb_layer_2.png",
                "res://duchess_assets/energy_counter/duchess_orb_layer_3.png",
                "res://duchess_assets/energy_counter/duchess_orb_layer_4.png",
                "res://duchess_assets/energy_counter/duchess_orb_layer_5.png",
                "res://duchess_assets/energy_icon/duchess_energy_card_icon.png",
                "res://images/atlases/ui_atlas.sprites/card/energy_duchess.tres",
                "res://images/packed/sprite_fonts/duchess_energy_icon.png",
                "res://duchess_assets/relics/duchess_old_pocketwatch.png",
                RestSiteLayoutPath,
                "res://duchess_assets/rest_site/duchess_rest_site.png",
                SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
                MerchantLayoutPath,
                "res://duchess_assets/merchant/duchess_merchant.png",
                SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
                "res://materials/transitions/duchess_transition_mat.tres",
                "res://duchess_assets/map_marker_duchess.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_duchess_point.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_duchess_rock.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_duchess_paper.png",
                $"{MultiplayerHandsPath}/multiplayer_hand_duchess_scissors.png",
                "res://duchess_assets/card_trail_duchess.tscn",
            };
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
        [HarmonyPostfix]
        public static void RestSiteVisuals(Player player, ref NRestSiteCharacter __result)
        {
            if (player?.Character is not Duchess || __result == null)
                return;

            if (__result.HasMeta("duchess_rest_site"))
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

            layout.Name = "DuchessRestVisual";
            __result.AddChild(layout);
            SetOwnerRecursively(layout, __result);
            __result.SetMeta("duchess_rest_site", true);
            layout.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
        }

        [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
        [HarmonyPostfix]
        public static void FlipRestSiteVisual(NRestSiteCharacter __instance)
        {
            if (!__instance.HasMeta("duchess_rest_site"))
                return;

            Node2D visual = __instance.GetNodeOrNull<Node2D>("DuchessRestVisual");
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
                if (____players[i].Character is Duchess)
                    ReplaceMerchantVisual(visuals[i]);
            }
        }

        private static void ReplaceMerchantVisual(NMerchantCharacter merchantCharacter)
        {
            if (merchantCharacter.HasMeta("duchess_merchant"))
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
            merchantCharacter.SetMeta("duchess_merchant", true);
            merchantCharacter.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
        [HarmonyPrefix]
        public static bool MerchantReady(NMerchantCharacter __instance)
        {
            if (!__instance.HasMeta("duchess_merchant"))
                return true;

            __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
                ?.Play("relaxed_loop");
            return false;
        }

        [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
        [HarmonyPrefix]
        public static bool MerchantAnimation(NMerchantCharacter __instance, string anim)
        {
            if (!__instance.HasMeta("duchess_merchant"))
                return true;

            AnimationPlayer player =
                __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            player?.Play(anim == "die" ? "die" : "relaxed_loop");
            return false;
        }
    }
}
