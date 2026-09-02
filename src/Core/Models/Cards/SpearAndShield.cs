using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class SpearAndShield : CardModel
    {
        private const string DefendCountKey = "DefendCount";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar(DefendCountKey).WithMultiplier(
                static (card, _) => PileType.Discard.GetPile(card.Owner).Cards.Count(
                    discardCard => discardCard is DefendGuardian))
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            GuardianCardHoverTips.Synthesis,
            HoverTipFactory.FromCard<ShieldPoke>(IsUpgraded)
        };

        protected override bool IsPlayable =>
            GuardianSynthesis.HasDefendSynthesisMaterials(Owner);

        public SpearAndShield()
            : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override void AddExtraArgsToDescription(LocString description)
        {
            description.Add(
                "GeneratedCard",
                ModelDb.Card<ShieldPoke>().Title + (IsUpgraded ? "+" : string.Empty));
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            CardPile discard = PileType.Discard.GetPile(base.Owner);
            if (!GuardianSynthesis.HasDefendSynthesisMaterials(base.Owner))
                return;

            CardModel defend = await GuardianSynthesis.SelectOneFromCombatPile(
                choiceContext,
                discard,
                card => card is DefendGuardian,
                this,
                "SPEAR_AND_SHIELD.defendSelectionPrompt");
            if (defend == null)
                return;

            CardModel other = await GuardianSynthesis.SelectOneFromCombatPile(
                choiceContext,
                discard,
                card => card != defend,
                this,
                "SPEAR_AND_SHIELD.otherSelectionPrompt");
            if (other == null)
                return;

            await CardCmd.Exhaust(choiceContext, defend);
            await CardCmd.Exhaust(choiceContext, other);

            CardModel shieldPoke = base.CombatState.CreateCard<ShieldPoke>(base.Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(shieldPoke);
            await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Hand, base.Owner);
        }

        protected override void OnUpgrade() { }
    }
}
