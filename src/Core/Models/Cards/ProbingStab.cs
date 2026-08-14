using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models;
using sts2mod.Core.Patches;

namespace sts2mod.Core.Models.Cards
{
    public sealed class ProbingStab : CardModel
    {
        private const string RetainCountKey = "RetainCount";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(6m, ValueProp.Move),
            new DynamicVar(RetainCountKey, 1m),
        };

        public ProbingStab()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            int retainCount = base.DynamicVars[RetainCountKey].IntValue;
            List<CardModel> candidates = PileType.Hand.GetPile(base.Owner).Cards
                .Where(card => GuardianCardFilters.HasDefendInName(card) && !card.Keywords.Contains(CardKeyword.Retain))
                .ToList();
            if (candidates.Count == 0)
                return;

            IEnumerable<CardModel> selected = candidates;
            if (candidates.Count > retainCount)
            {
                selected = await CardSelectCmd.FromHand(
                    choiceContext,
                    base.Owner,
                    new CardSelectorPrefs(new LocString("cards", "PROBING_STAB.selectionScreenPrompt"), retainCount),
                    card => GuardianCardFilters.HasDefendInName(card) && !card.Keywords.Contains(CardKeyword.Retain),
                    this);
            }

            foreach (CardModel card in selected.ToList())
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Retain);
                TransientCardKeywordRegistry.TrackRetain(card);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(3m);
            base.DynamicVars[RetainCountKey].UpgradeValueBy(1m);
        }
    }
}
