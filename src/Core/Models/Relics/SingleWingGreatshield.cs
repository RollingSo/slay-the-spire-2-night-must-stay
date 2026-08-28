using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Relics
{
    public sealed class SingleWingGreatshield : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        public override string PackedIconPath => "res://guardian_assets/relics/single_wing_greatshield.png";

        protected override string PackedIconOutlinePath => PackedIconPath;

        protected override string BigIconPath => PackedIconPath;

        public override bool ShouldFlashOnPlayer => false;

        public override async Task BeforeCombatStart()
        {
            Flash();
            await PowerCmd.Apply<FortifyPower>(new BlockingPlayerChoiceContext(), base.Owner.Creature, 5m, base.Owner.Creature, null);
        }
    }

    public sealed class TwinWingGreatshield : RelicModel
    {
        private const string FortifyKey = "Fortify";

        public override RelicRarity Rarity => RelicRarity.Ancient;

        public override string PackedIconPath => "res://guardian_assets/relics/twin_wing_greatshield.png";

        protected override string PackedIconOutlinePath => PackedIconPath;

        protected override string BigIconPath => PackedIconPath;

        public override bool ShouldFlashOnPlayer => false;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(10m, ValueProp.Unpowered),
            new PowerVar<FortifyPower>(FortifyKey, 10m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<FortifyPower>()
        };

        public override async Task BeforeCombatStart()
        {
            Flash();
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
            await PowerCmd.Apply<FortifyPower>(
                new BlockingPlayerChoiceContext(),
                base.Owner.Creature,
                base.DynamicVars[FortifyKey].BaseValue,
                base.Owner.Creature,
                null);
        }
    }
}
