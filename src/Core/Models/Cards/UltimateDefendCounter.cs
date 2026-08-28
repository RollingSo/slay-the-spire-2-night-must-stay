using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class UltimateDefendCounter : CardModel
    {
        private const string GuardCounterKey = "GuardCounter";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/ultimate_defend_counter.png");

        public override bool GainsBlock => true;

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
        {
            CardKeyword.Exhaust
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<GuardCounterPower>(GuardCounterKey, 18m),
            new BlockVar(12m, ValueProp.Move)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        };

        public UltimateDefendCounter()
            : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PowerCmd.Apply<GuardCounterPower>(
                context,
                Owner.Creature,
                DynamicVars[GuardCounterKey].BaseValue,
                Owner.Creature,
                this);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }

        protected override void OnUpgrade()
        {
            DynamicVars[GuardCounterKey].UpgradeValueBy(4m);
            DynamicVars.Block.UpgradeValueBy(3m);
        }
    }
}
