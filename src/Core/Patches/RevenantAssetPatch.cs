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

    [HarmonyPatch(typeof(CardModel), "get_EnergyIconPath")]
    [HarmonyPostfix]
    public static void RevenantEnergyIcon(CardModel __instance, ref string __result)
    {
        if (__instance?.VisualCardPool?.Title == "revenant")
            __result = "res://images/atlases/ui_atlas.sprites/card/energy_revenant.tres";
    }

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

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
    [HarmonyPostfix]
    public static void AttackSfx(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = "event:/sfx/ui/cards/card_impact_into_single";
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CastSfx(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = "event:/sfx/ui/cards/card_exhaust";
    }

    [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)]
    [HarmonyPostfix]
    public static void DeathSfx(CharacterModel __instance, ref string __result)
    {
        if (__instance is Revenant)
            __result = "event:/sfx/ui/combat/end_turn";
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
            "res://images/packed/sprite_fonts/revenant_energy_icon.png",
            "res://revenant_assets/relics/revenant_starter_relic.png",
            "res://revenant_assets/cards/cursed_claw_combo.png",
            "res://revenant_assets/cards/strike_revenant.png",
            "res://revenant_assets/cards/defend_revenant.png",
            "res://revenant_assets/cards/resonance.png",
            "res://revenant_assets/cards/call.png",
            "res://revenant_assets/cards/halo.png",
            "res://revenant_assets/cards/emergency_restore.png",
            "res://revenant_assets/cards/precise_lightning_strike.png",
            "res://revenant_assets/cards/threefold_halo.png",
            "res://revenant_assets/cards/ancient_dragon_lightning.png",
            "res://revenant_assets/cards/lansseax_blade.png",
            "res://revenant_assets/cards/lightning_strike.png",
            "res://revenant_assets/cards/ancient_dragon_spear.png",
            "res://revenant_assets/cards/recover.png",
            "res://revenant_assets/cards/flannsax_lightning_spear.png",
            "res://revenant_assets/cards/beast_claw.png",
            "res://revenant_assets/cards/death_lightning.png",
            "res://revenant_assets/cards/space_rending_frenzy.png",
            "res://revenant_assets/cards/white_shadow_lure.png",
            "res://revenant_assets/cards/soulguard.png",
            "res://revenant_assets/cards/lightning_spear.png",
            "res://revenant_assets/cards/spirit_form.png",
            "res://revenant_assets/cards/unbearable_frenzy.png",
            "res://revenant_assets/cards/beaststone.png",
            "res://revenant_assets/cards/radagon_halo.png",
            "res://revenant_assets/cards/soul_summon.png",
            "res://revenant_assets/cards/grave_rob.png",
            "res://revenant_assets/cards/greater_recover.png",
            "res://revenant_assets/cards/ancient_dragon_faith.png",
            "res://revenant_assets/cards/beast_claw_mark.png",
            "res://revenant_assets/cards/golden_order.png",
            "res://revenant_assets/cards/spirit_link.png",
            "res://revenant_assets/cards/blessing_of_grace.png",
            "res://revenant_assets/cards/gurranq_beast_claw.png",
            "res://revenant_assets/cards/gurranqs_rock.png",
            "res://revenant_assets/cards/frenzied_flame.png",
            "res://revenant_assets/cards/kings_recovery.png",
            "res://revenant_assets/cards/undying_march.png",
            "res://revenant_assets/cards/helen_family.png",
            "res://revenant_assets/cards/pumpkin_head_family.png",
            "res://revenant_assets/cards/skeleton_family.png",
            "res://revenant_assets/cards/ensemble.png",
            "res://revenant_assets/cards/surge.png",
            "res://revenant_assets/cards/underworld_rising.png",
            "res://revenant_assets/cards/resurgence.png",
            "res://revenant_assets/cards/answer_the_call.png",
            "res://revenant_assets/cards/revenant_card.png",
            "res://revenant_assets/cards/soulbound.png",
            "res://revenant_assets/cards/frenzied_three_fingers.png",
            "res://revenant_assets/cards/formation_breaker_hammer.png",
            "res://revenant_assets/cards/life_and_death.png",
            "res://revenant_assets/cards/giant_skeleton_wrath.png",
            "res://revenant_assets/cards/sky_rending_chord.png",
            "res://revenant_assets/cards/substitute_doll.png",
            "res://revenant_assets/cards/spirit_gathering.png",
            "res://revenant_assets/cards/concerto.png",
            "res://revenant_assets/cards/fight_for_me.png",
            "res://revenant_assets/cards/soul_cursing_bell.png",
            "res://revenant_assets/cards/light_spirit.png",
            "res://revenant_assets/cards/grooming.png",
            "res://revenant_assets/cards/reanimate_dead.png",
            "res://revenant_assets/cards/soul_return.png",
            "res://revenant_assets/cards/heavy_echo.png",
            "res://revenant_assets/cards/chanting_blessing.png",
            "res://revenant_assets/cards/underworld_reflection.png",
            "res://revenant_assets/cards/spirit_manipulation.png",
            "res://revenant_assets/cards/preparation_ritual.png",
            "res://revenant_assets/cards/watchful_waiting.png",
            "res://revenant_assets/cards/all_souls_return.png",
            "res://revenant_assets/cards/following_shadow.png",
            "res://revenant_assets/families/helen.png",
            "res://revenant_assets/families/frederick.png",
            "res://revenant_assets/families/sebastian.png",
            "res://revenant_assets/families/necro.png",
            "res://revenant_assets/powers/revenant_summon_controller_power.png",
            "res://revenant_assets/powers/helen_step_strike_power.png",
            "res://revenant_assets/powers/helen_retreat_power.png",
            "res://revenant_assets/powers/frederick_heavy_hammer_power.png",
            "res://revenant_assets/powers/frederick_headbutt_power.png",
            "res://revenant_assets/powers/sebastian_roar_power.png",
            "res://revenant_assets/powers/sebastian_slam_power.png",
            "res://revenant_assets/powers/freeze_power.png",
            "res://revenant_assets/powers/white_shadow_lure_power.png",
            "res://revenant_assets/powers/soulguard_power.png",
            "res://revenant_assets/powers/spirit_form_power.png",
            "res://revenant_assets/powers/ancient_dragon_faith_power.png",
            "res://revenant_assets/powers/beast_claw_mark_power.png",
            "res://revenant_assets/powers/golden_order_power.png",
            "res://revenant_assets/powers/blessing_of_grace_power.png",
            "res://revenant_assets/powers/frenzied_three_fingers_power.png",
            "res://revenant_assets/powers/fight_for_me_power.png",
            "res://revenant_assets/powers/light_spirit_power.png",
            "res://revenant_assets/powers/heavy_echo_power.png",
            "res://revenant_assets/powers/chanting_blessing_power.png",
            "res://revenant_assets/powers/following_shadow_power.png",
            "res://revenant_assets/powers/necromancy_power.png",
            "res://images/powers/helen_step_strike_power.png",
            "res://images/powers/helen_retreat_power.png",
            "res://images/powers/frederick_heavy_hammer_power.png",
            "res://images/powers/frederick_headbutt_power.png",
            "res://images/powers/sebastian_roar_power.png",
            "res://images/powers/sebastian_slam_power.png",
            "res://images/powers/freeze_power.png",
            "res://images/powers/die_for_you_power.png",
            "res://images/powers/white_shadow_lure_power.png",
            "res://images/powers/soulguard_power.png",
            "res://images/powers/spirit_form_power.png",
            "res://images/powers/ancient_dragon_faith_power.png",
            "res://images/powers/beast_claw_mark_power.png",
            "res://images/powers/golden_order_power.png",
            "res://images/powers/blessing_of_grace_power.png",
            "res://images/powers/frenzied_three_fingers_power.png",
            "res://images/powers/fight_for_me_power.png",
            "res://images/powers/light_spirit_power.png",
            "res://images/powers/heavy_echo_power.png",
            "res://images/powers/chanting_blessing_power.png",
            "res://images/powers/following_shadow_power.png",
            "res://images/powers/necromancy_power.png",
            "res://images/atlases/power_atlas.sprites/die_for_you_power.tres",
            "res://images/atlases/power_atlas.sprites/helen_step_strike_power.tres",
            "res://images/atlases/power_atlas.sprites/helen_retreat_power.tres",
            "res://images/atlases/power_atlas.sprites/frederick_heavy_hammer_power.tres",
            "res://images/atlases/power_atlas.sprites/frederick_headbutt_power.tres",
            "res://images/atlases/power_atlas.sprites/sebastian_roar_power.tres",
            "res://images/atlases/power_atlas.sprites/sebastian_slam_power.tres",
            "res://images/atlases/power_atlas.sprites/freeze_power.tres",
            "res://images/atlases/power_atlas.sprites/white_shadow_lure_power.tres",
            "res://images/atlases/power_atlas.sprites/soulguard_power.tres",
            "res://images/atlases/power_atlas.sprites/spirit_form_power.tres",
            "res://images/atlases/power_atlas.sprites/ancient_dragon_faith_power.tres",
            "res://images/atlases/power_atlas.sprites/beast_claw_mark_power.tres",
            "res://images/atlases/power_atlas.sprites/golden_order_power.tres",
            "res://images/atlases/power_atlas.sprites/blessing_of_grace_power.tres",
            "res://images/atlases/power_atlas.sprites/frenzied_three_fingers_power.tres",
            "res://images/atlases/power_atlas.sprites/fight_for_me_power.tres",
            "res://images/atlases/power_atlas.sprites/light_spirit_power.tres",
            "res://images/atlases/power_atlas.sprites/heavy_echo_power.tres",
            "res://images/atlases/power_atlas.sprites/chanting_blessing_power.tres",
            "res://images/atlases/power_atlas.sprites/following_shadow_power.tres",
            "res://images/atlases/power_atlas.sprites/necromancy_power.tres",
            "res://images/atlases/intent_atlas.sprites/intent_defend.tres",
            "res://images/atlases/intent_atlas.sprites/intent_debuff.tres",
            "res://scenes/creature_visuals/osty.tscn",
            "res://scenes/vfx/vfx_heal_osty.tscn",
            "res://materials/cards/frames/card_frame_revenant_mat.tres",
            RestVisuals,
            "res://revenant_assets/rest_site/revenant_rest_site.png",
            MerchantVisuals,
            "res://revenant_assets/merchant/revenant_merchant.png",
            "res://revenant_assets/multiplayer_hands/revenant_point.png",
            "res://revenant_assets/multiplayer_hands/revenant_rock.png",
            "res://revenant_assets/multiplayer_hands/revenant_paper.png",
            "res://revenant_assets/multiplayer_hands/revenant_scissors.png",
            CardTrail,
            SceneHelper.GetScenePath("vfx/card_trail_ironclad"),
            SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter"),
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
