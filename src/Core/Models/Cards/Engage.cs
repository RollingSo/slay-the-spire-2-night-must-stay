using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class Engage : CardModel
    {
        public override bool GainsBlock => true;

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromCard<ShieldPoke>(base.IsUpgraded)
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(8m, ValueProp.Move),
        };

        public Engage()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override void AddExtraArgsToDescription(LocString description)
        {
            description.Add("GeneratedCard", GetGeneratedCardTitle());
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

            CardModel shieldPoke = base.CombatState.CreateCard<ShieldPoke>(base.Owner);
            if (base.IsUpgraded)
            {
                CardCmd.Upgrade(shieldPoke);
            }

            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Draw, base.Owner, CardPilePosition.Random));
        }

        private string GetGeneratedCardTitle()
        {
            CardModel card = ModelDb.Card<ShieldPoke>();
            if (base.IsUpgraded)
            {
                card = (CardModel)card.MutableClone();
                card.UpgradeInternal();
            }
            return card.Title;
        }
    }
}
