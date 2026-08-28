using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class DefensiveReinforcement : CardModel
    {
        public override bool GainsBlock => true;

        public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
        {
            CardKeyword.Retain,
            CardKeyword.Exhaust,
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(0m, ValueProp.Move),
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar("CalculatedBlock").WithMultiplier(
                (card, _) => card.Owner.Creature.GetPower<FortifyPower>()?.Amount ?? 0m)
        };

        public DefensiveReinforcement()
            : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            decimal calculatedBlock = ((CalculatedVar)base.DynamicVars["CalculatedBlock"]).Calculate(null);
            if (calculatedBlock > 0m)
            {
                base.DynamicVars.Block.BaseValue = calculatedBlock;
                await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            base.EnergyCost.UpgradeBy(-1);
        }
    }
}
