using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.PotionPools;
using NightMustStay.Core.Models.RelicPools;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.Characters;

public sealed class Revenant : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Feminine;
    protected override CharacterModel UnlocksAfterRunAs => null;
    public override Color NameColor => new("B8A0DA");
    protected override string CharacterSelectIconPath => "res://revenant_assets/char_select_revenant.png";
    protected override string CharacterSelectLockedIconPath => "res://revenant_assets/char_select_revenant_locked.png";
    protected override string IconPath => "res://revenant_assets/character_icon_revenant.tscn";
    protected override string MapMarkerPath => "res://revenant_assets/map_marker_revenant.png";

    public override int StartingHp => 68;
    public override int StartingGold => 99;
    public override CardPoolModel CardPool => ModelDb.CardPool<RevenantCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<RevenantPotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<RevenantRelicPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<StrikeRevenant>(), ModelDb.Card<StrikeRevenant>(),
        ModelDb.Card<StrikeRevenant>(), ModelDb.Card<StrikeRevenant>(),
        ModelDb.Card<DefendRevenant>(), ModelDb.Card<DefendRevenant>(),
        ModelDb.Card<DefendRevenant>(), ModelDb.Card<DefendRevenant>(),
        ModelDb.Card<RevenantCall>(), ModelDb.Card<RevenantResonance>(),
    };

    public override IReadOnlyList<RelicModel> StartingRelics =>
        new[] { ModelDb.Relic<SmallMakeupBrush>() };
    public override string CharacterSelectSfx => "event:/sfx/ui/clicks/ui_click";
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_map";
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    public override Color EnergyLabelOutlineColor => new("21172EFF");
    public override Color DialogueColor => new("30243D");
    public override VfxColor SpeechBubbleColor => VfxColor.Purple;
    public override Color MapDrawingColor => new("8F74B7");
    public override Color RemoteTargetingLineColor => new("D7BDF5FF");
    public override Color RemoteTargetingLineOutline => new("21172EFF");
    public override List<string> GetArchitectAttackVfx() => new()
    {
        "vfx/vfx_attack_lightning",
        "vfx/vfx_starry_impact",
        "vfx/vfx_thrash",
        "vfx/vfx_attack_slash",
    };
}
