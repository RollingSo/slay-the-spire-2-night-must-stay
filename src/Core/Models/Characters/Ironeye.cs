using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using sts2mod.Core.Models.CardPools;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.PotionPools;
using sts2mod.Core.Models.RelicPools;
using sts2mod.Core.Models.Relics;

namespace sts2mod.Core.Models.Characters
{
    public sealed class Ironeye : CharacterModel
    {
        public override CharacterGender Gender => CharacterGender.Masculine;

        protected override CharacterModel UnlocksAfterRunAs => null;

        public override Color NameColor => new("9AA35D");

        protected override string CharacterSelectIconPath => "res://ironeye_assets/char_select_ironeye.png";

        protected override string CharacterSelectLockedIconPath => "res://ironeye_assets/char_select_ironeye_locked.png";

        protected override string IconPath => "res://ironeye_assets/character_icon_ironeye.tscn";

        protected override string MapMarkerPath => "res://ironeye_assets/map_marker_ironeye.png";

        public override int StartingHp => 70;

        public override int StartingGold => 99;

        public override CardPoolModel CardPool => ModelDb.CardPool<IroneyeCardPool>();

        public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroneyePotionPool>();

        public override RelicPoolModel RelicPool => ModelDb.RelicPool<IroneyeRelicPool>();

        public override IEnumerable<CardModel> StartingDeck => new CardModel[]
        {
            ModelDb.Card<StrikeIroneye>(),
            ModelDb.Card<StrikeIroneye>(),
            ModelDb.Card<StrikeIroneye>(),
            ModelDb.Card<StrikeIroneye>(),
            ModelDb.Card<DefendIroneye>(),
            ModelDb.Card<DefendIroneye>(),
            ModelDb.Card<DefendIroneye>(),
            ModelDb.Card<DefendIroneye>(),
            ModelDb.Card<IroneyeMark>(),
            ModelDb.Card<BackstepShot>(),
        };

        public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
        {
            ModelDb.Relic<CursemarkSignet>(),
        };

        public override string CharacterSelectSfx => "event:/sfx/ui/clicks/ui_click";

        public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_map";

        public override float AttackAnimDelay => 0.15f;

        public override float CastAnimDelay => 0.25f;

        public override Color EnergyLabelOutlineColor => new("29351FFF");

        public override Color DialogueColor => new("242D1C");

        public override VfxColor SpeechBubbleColor => VfxColor.Swamp;

        public override Color MapDrawingColor => new("74814A");

        public override Color RemoteTargetingLineColor => new("C8D94AFF");

        public override Color RemoteTargetingLineOutline => new("29351FFF");

        public override List<string> GetArchitectAttackVfx()
        {
            return new List<string>
            {
                "vfx/vfx_flying_slash",
                "vfx/vfx_dramatic_stab",
                "vfx/vfx_dagger_throw",
                "vfx/vfx_attack_slash",
            };
        }
    }
}
