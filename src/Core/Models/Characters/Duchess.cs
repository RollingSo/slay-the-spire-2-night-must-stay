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

public sealed class Duchess : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Feminine;
    protected override CharacterModel UnlocksAfterRunAs => null;
    public override Color NameColor => new("B1C8EB");
    protected override string CharacterSelectIconPath => "res://duchess_assets/char_select_duchess.png";
    protected override string CharacterSelectLockedIconPath => "res://duchess_assets/char_select_duchess_locked.png";
    protected override string IconPath => "res://duchess_assets/character_icon_duchess.tscn";
    protected override string MapMarkerPath => "res://duchess_assets/map_marker_duchess.png";
    public override int StartingHp => 66;
    public override int StartingGold => 99;
    public override CardPoolModel CardPool => ModelDb.CardPool<DuchessCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DuchessPotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DuchessRelicPool>();
    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<DuchessStrike>(), ModelDb.Card<DuchessStrike>(),
        ModelDb.Card<DuchessStrike>(), ModelDb.Card<DuchessStrike>(),
        ModelDb.Card<DuchessDefend>(), ModelDb.Card<DuchessDefend>(),
        ModelDb.Card<DuchessDefend>(), ModelDb.Card<DuchessDefend>(),
        ModelDb.Card<DuchessElegantBearing>(), ModelDb.Card<DuchessBladeRevealMoment>(),
    };
    public override IReadOnlyList<RelicModel> StartingRelics => new[] { ModelDb.Relic<DuchessOldPocketwatch>() };
    public override string CharacterSelectSfx => "event:/sfx/ui/clicks/ui_click";
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_map";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override Color EnergyLabelOutlineColor => new("17243D");
    public override Color DialogueColor => new("253654");
    public override VfxColor SpeechBubbleColor => VfxColor.Purple;
    public override Color MapDrawingColor => new("92ACD6");
    public override Color RemoteTargetingLineColor => new("B6D7FF");
    public override Color RemoteTargetingLineOutline => new("17243D");
    public override List<string> GetArchitectAttackVfx() => new()
    { "vfx/vfx_flying_slash", "vfx/vfx_dramatic_stab", "vfx/vfx_dagger_throw", "vfx/vfx_attack_slash" };
}
