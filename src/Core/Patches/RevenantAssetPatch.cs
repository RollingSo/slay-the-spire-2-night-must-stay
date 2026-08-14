using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using sts2mod.Core.Models.Characters;

namespace sts2mod.Core.Patches;

[HarmonyPatch]
public static class RevenantAssetPatch
{
    private const string CombatVisuals = "res://revenant_assets/combat/revenant_combat_visuals.tscn";
    private const string RestVisuals = "res://revenant_assets/rest_site/revenant_rest_site.tscn";
    private const string MerchantVisuals = "res://revenant_assets/merchant/revenant_merchant.tscn";
    private const string CardTrail = "res://revenant_assets/card_trail_revenant.tscn";

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CharacterSelectBg(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = "res://revenant_assets/char_select_bg_revenant.tscn";
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Transition(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = "res://materials/transitions/revenant_transition_mat.tres";
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)]
    [HarmonyPostfix]
    public static void EnergyCounter(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter");
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Merchant(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant");
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)]
    [HarmonyPostfix]
    public static void RestSite(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site");
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Trail(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = CardTrail;
    }

    [HarmonyPatch(typeof(NCardTrailVfx), nameof(NCardTrailVfx.Create))]
    [HarmonyPrefix]
    public static bool CreateCardTrail(Control card, string characterTrailPath, ref NCardTrailVfx __result)
    {
        if (characterTrailPath != CardTrail)
            return true;

        __result = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath("vfx/card_trail_ironclad"))
            .Instantiate<NCardTrailVfx>(PackedScene.GenEditState.Disabled);
        AccessTools.Field(typeof(NCardTrailVfx), "_nodeToFollow")?.SetValue(__result, card);

        Line2D outer = __result.GetNodeOrNull<Line2D>("Trails/OuterTrail");
        Line2D inner = __result.GetNodeOrNull<Line2D>("Trails/InnerTrail");
        if (outer != null)
        {
            outer.Modulate = new Color("8E75BFD0");
            outer.Gradient = new Gradient
            {
                Colors = new[] { new Color("261B3500"), new Color("9D7BD4C8"), new Color("F0D8FFFF") },
            };
        }
        if (inner != null)
        {
            inner.Modulate = new Color("D8BEF0D8");
            inner.Gradient = new Gradient
            {
                Colors = new[] { new Color("6E4F9100"), new Color("CFADF0D8"), new Color("FFF3FFFF") },
            };
        }

        Texture2D emblem = PreloadManager.Cache.GetTexture2D(
            "res://revenant_assets/energy/revenant_energy_font_icon.png");
        Node2D sprites = __result.GetNodeOrNull<Node2D>("Sprites");
        if (sprites != null)
        {
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
                    sprite.Modulate = new Color("E3CBF4DD");
                    sprite.Scale = Vector2.One * 0.72f;
                }
            }
        }
        return false;
    }

    [HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter.Create))]
    [HarmonyPostfix]
    public static void EnergyTextures(Player player, ref NEnergyCounter __result)
    {
        if (player?.Character is not Revenant || __result == null) return;
        Control layers = __result.GetNode<Control>("%Layers");
        layers.GetNode<TextureRect>("Layer1").Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/energy/revenant_orb_layer_1.png");
        layers.GetNode<TextureRect>("RotationLayers/Layer2").Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/energy/revenant_orb_layer_2.png");
        layers.GetNode<TextureRect>("RotationLayers/Layer3").Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/energy/revenant_orb_layer_3.png");
        layers.GetNode<TextureRect>("Layer4").Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/energy/revenant_orb_layer_4.png");
        layers.GetNode<TextureRect>("Layer5").Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/energy/revenant_orb_layer_5.png");
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Icon(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not Revenant) return true;
        __result = PreloadManager.Cache.GetTexture2D("res://revenant_assets/character_icon_revenant.png");
        return false;
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool IconOutline(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not Revenant) return true;
        __result = PreloadManager.Cache.GetTexture2D("res://revenant_assets/character_icon_revenant_outline.png");
        return false;
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Pointing(CharacterModel __instance, ref Texture2D __result) => Hand(__instance, "point", ref __result);
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Rock(CharacterModel __instance, ref Texture2D __result) => Hand(__instance, "rock", ref __result);
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Paper(CharacterModel __instance, ref Texture2D __result) => Hand(__instance, "paper", ref __result);
    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Scissors(CharacterModel __instance, ref Texture2D __result) => Hand(__instance, "scissors", ref __result);

    private static bool Hand(CharacterModel character, string gesture, ref Texture2D result)
    {
        if (character is not Revenant) return true;
        result = PreloadManager.Cache.GetTexture2D($"res://revenant_assets/multiplayer_hands/revenant_{gesture}.png");
        return false;
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
    [HarmonyPostfix]
    public static void MapMarker(CharacterModel __instance, ref CompressedTexture2D __result)
    {
        if (__instance is Revenant)
            __result = PreloadManager.Cache.GetCompressedTexture2D("res://revenant_assets/map_marker_revenant.png");
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
    [HarmonyPrefix]
    public static bool CreateVisuals(CharacterModel __instance, ref NCreatureVisuals __result)
    {
        if (__instance is not Revenant) return true;
        Node2D layout = PreloadManager.Cache.GetScene(CombatVisuals)
            .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        var visuals = new NCreatureVisuals { Name = "RevenantCombatVisuals" };
        while (layout.GetChildCount() > 0)
        {
            Node child = layout.GetChild(0);
            layout.RemoveChild(child);
            visuals.AddChild(child);
            SetOwner(child, visuals);
        }
        layout.Free();
        __result = visuals;
        return false;
    }

    private static void SetOwner(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (Node child in node.GetChildren()) SetOwner(child, owner);
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPathsCharacterSelect), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CharacterSelectAssets(CharacterModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is not Revenant) return;
        __result = new[]
        {
            "res://revenant_assets/char_select_bg_revenant.tscn",
            "res://revenant_assets/character_select_revenant_bg.png",
            "res://revenant_assets/char_select_revenant.png",
            "res://revenant_assets/char_select_revenant_locked.png",
            "res://revenant_assets/character_icon_revenant.png",
            "res://revenant_assets/vfx/revenant_spirit_mote.svg",
            "res://materials/transitions/revenant_transition_mat.tres",
        };
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AssetPaths), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Assets(CharacterModel __instance, ref IEnumerable<string> __result)
    {
        if (__instance is not Revenant) return;
        __result = new[]
        {
            CombatVisuals,
            "res://revenant_assets/combat/revenant_combat_rig.tscn",
            "res://revenant_assets/combat/revenant_combat_rig.gd",
            "res://revenant_assets/combat/revenant_idle.png",
            "res://revenant_assets/combat/revenant_attack.png",
            "res://revenant_assets/combat/revenant_hit.png",
            "res://revenant_assets/character_icon_revenant.png",
            "res://revenant_assets/character_icon_revenant_outline.png",
            "res://revenant_assets/character_icon_revenant.tscn",
            "res://revenant_assets/map_marker_revenant.png",
            "res://revenant_assets/energy/revenant_energy.png",
            "res://revenant_assets/energy/revenant_orb_layer_1.png",
            "res://revenant_assets/energy/revenant_orb_layer_2.png",
            "res://revenant_assets/energy/revenant_orb_layer_3.png",
            "res://revenant_assets/energy/revenant_orb_layer_4.png",
            "res://revenant_assets/energy/revenant_orb_layer_5.png",
            "res://images/atlases/ui_atlas.sprites/card/energy_revenant.tres",
            "res://revenant_assets/energy/revenant_energy_font_icon.png",
            "res://revenant_assets/relics/revenant_starter_relic.png",
            RestVisuals,
            "res://revenant_assets/rest_site/revenant_rest_site.png",
            MerchantVisuals,
            "res://revenant_assets/merchant/revenant_merchant.png",
            "res://revenant_assets/multiplayer_hands/revenant_point.png",
            "res://revenant_assets/multiplayer_hands/revenant_rock.png",
            "res://revenant_assets/multiplayer_hands/revenant_paper.png",
            "res://revenant_assets/multiplayer_hands/revenant_scissors.png",
            CardTrail,
            SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
            SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
        };
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
    [HarmonyPostfix]
    public static void RestSiteVisuals(Player player, ref NRestSiteCharacter __result)
    {
        if (player?.Character is not Revenant || __result == null || __result.HasMeta("revenant_visuals")) return;
        foreach (Node child in __result.GetChildren())
        {
            if (child.GetClass() != "SpineSprite") continue;
            __result.RemoveChild(child);
            child.QueueFree();
        }
        Node2D layout = PreloadManager.Cache.GetScene(RestVisuals).Instantiate<Node2D>();
        layout.Name = "RevenantRestVisuals";
        __result.AddChild(layout);
        SetOwner(layout, __result);
        __result.SetMeta("revenant_visuals", true);
        layout.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("idle");
    }

    [HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
    [HarmonyPostfix]
    public static void MerchantVisualsPatch(NMerchantRoom __instance, List<Player> ____players)
    {
        int count = Math.Min(__instance.PlayerVisuals.Count, ____players.Count);
        for (int i = 0; i < count; i++)
        {
            if (____players[i].Character is not Revenant) continue;
            NMerchantCharacter character = __instance.PlayerVisuals[i];
            while (character.GetChildCount() > 0)
            {
                Node child = character.GetChild(0);
                character.RemoveChild(child);
                child.QueueFree();
            }
            Node2D layout = PreloadManager.Cache.GetScene(MerchantVisuals).Instantiate<Node2D>();
            while (layout.GetChildCount() > 0)
            {
                Node child = layout.GetChild(0);
                layout.RemoveChild(child);
                character.AddChild(child);
                SetOwner(child, character);
            }
            layout.Free();
            character.SetMeta("revenant_visuals", true);
            character.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("relaxed_loop");
        }
    }

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    public static bool MerchantReady(NMerchantCharacter __instance) =>
        !__instance.HasMeta("revenant_visuals");

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
    [HarmonyPrefix]
    public static bool MerchantAnimation(NMerchantCharacter __instance, string anim)
    {
        if (!__instance.HasMeta("revenant_visuals"))
            return true;

        __instance.GetNodeOrNull<AnimationPlayer>("AnimationPlayer")
            ?.Play(anim == "die" ? "die" : "relaxed_loop");
        return false;
    }
}
