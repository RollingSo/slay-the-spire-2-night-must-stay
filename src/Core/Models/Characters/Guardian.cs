using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using sts2mod.Core.Models.CardPools;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.RelicPools;
using sts2mod.Core.Models.Relics;
using sts2mod.Core.Models.PotionPools;

namespace sts2mod.Core.Models.Characters
{
    public sealed class Guardian : CharacterModel
    {
        public override CharacterGender Gender => CharacterGender.Masculine;

        protected override CharacterModel UnlocksAfterRunAs => null;

        public override Color NameColor => StsColors.blue;

        protected override string CharacterSelectIconPath => "res://guardian_assets/char_select_guardian.png";

        protected override string CharacterSelectLockedIconPath => "res://guardian_assets/char_select_guardian_locked.png";

        protected override string IconPath => "res://guardian_assets/character_icon_guardian.tscn";

        protected override string MapMarkerPath => "res://guardian_assets/map_marker_guardian.png";

        public override int StartingHp => 80;

        public override int StartingGold => 99;

        public override CardPoolModel CardPool => ModelDb.CardPool<GuardianCardPool>();

        public override PotionPoolModel PotionPool => ModelDb.PotionPool<GuardianPotionPool>();

        public override RelicPoolModel RelicPool => ModelDb.RelicPool<GuardianRelicPool>();

        public override IEnumerable<CardModel> StartingDeck => new CardModel[]
        {
            ModelDb.Card<StrikeGuardian>(),
            ModelDb.Card<StrikeGuardian>(),
            ModelDb.Card<StrikeGuardian>(),
            ModelDb.Card<StrikeGuardian>(),
            ModelDb.Card<DefendGuardian>(),
            ModelDb.Card<DefendGuardian>(),
            ModelDb.Card<DefendGuardian>(),
            ModelDb.Card<DefendGuardian>(),
            ModelDb.Card<GuardCounterCard>(),
            ModelDb.Card<StompStance>(),
        };

        public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
        {
            ModelDb.Relic<SingleWingGreatshield>(),
        };

        public override string CharacterSelectSfx => "event:/sfx/ui/clicks/ui_click";

        public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_map";

        public override float AttackAnimDelay => 0.15f;

        public override float CastAnimDelay => 0.25f;

        public override Color EnergyLabelOutlineColor => new Color("1A3A5CFF");

        public override Color DialogueColor => new Color("0D1B2A");

        public override VfxColor SpeechBubbleColor => VfxColor.Cyan;

        public override Color MapDrawingColor => new Color("3A6EA5");

        public override Color RemoteTargetingLineColor => new Color("4A8EC5FF");

        public override Color RemoteTargetingLineOutline => new Color("1A3A5CFF");

        public override List<string> GetArchitectAttackVfx()
        {
            return new List<string>
            {
                "vfx/vfx_attack_blunt",
                "vfx/vfx_heavy_blunt",
                "vfx/vfx_attack_slash",
                "vfx/vfx_bloody_impact",
                "vfx/vfx_rock_shatter"
            };
        }
    }
}
