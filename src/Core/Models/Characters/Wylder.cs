using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.PotionPools;
using NightMustStay.Core.Models.RelicPools;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.Characters;

public sealed class Wylder : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Masculine;
    protected override CharacterModel UnlocksAfterRunAs => null;
    public override Color NameColor => new("E2AE72");
    protected override string CharacterSelectIconPath => "res://wylder_assets/char_select_wylder.png";
    protected override string CharacterSelectLockedIconPath => "res://wylder_assets/char_select_wylder_locked.png";
    protected override string IconPath => "res://wylder_assets/character_icon_wylder.tscn";
    protected override string MapMarkerPath => "res://wylder_assets/map_marker_wylder.png";
    public override int StartingHp => 80;
    public override int StartingGold => 99;
    public override CardPoolModel CardPool => ModelDb.CardPool<WylderCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<WylderPotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<WylderRelicPool>();
    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<WylderStrike>(), ModelDb.Card<WylderStrike>(),
        ModelDb.Card<WylderStrike>(), ModelDb.Card<WylderStrike>(),
        ModelDb.Card<WylderDefend>(), ModelDb.Card<WylderDefend>(),
        ModelDb.Card<WylderDefend>(), ModelDb.Card<WylderDefend>(),
        ModelDb.Card<WylderClawMovement>(), ModelDb.Card<WylderDashAttack>(),
    };
    public override IReadOnlyList<RelicModel> StartingRelics => new[] { ModelDb.Relic<WylderSingleEarring>() };
    public override string CharacterSelectSfx => "event:/sfx/ui/clicks/ui_click";
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_map";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override Color EnergyLabelOutlineColor => new("17243D");
    public override Color DialogueColor => new("253654");
    public override VfxColor SpeechBubbleColor => VfxColor.Purple;
    public override Color MapDrawingColor => NightMustStay.Core.CharacterThemeColors.Wylder;
    public override Color RemoteTargetingLineColor => new("EEAE62");
    public override Color RemoteTargetingLineOutline => new("17243D");
    public override List<string> GetArchitectAttackVfx() => new()
    { "vfx/vfx_flying_slash", "vfx/vfx_dramatic_stab", "vfx/vfx_dagger_throw", "vfx/vfx_attack_slash" };
}
