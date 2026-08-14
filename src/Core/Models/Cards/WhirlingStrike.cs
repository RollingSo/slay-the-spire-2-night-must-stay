using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards
{
    public sealed class WhirlingStrike : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(5m, ValueProp.Move),
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar("CalculatedHits").WithMultiplier((card, _) => PileType.Hand.GetPile(card.Owner).Cards.Count(GuardianCardFilters.HasDefendInName))
        };

        public WhirlingStrike()
            : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int defendCardsInHand = (int)((CalculatedVar)base.DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target);
            if (defendCardsInHand == 0)
                return;

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .WithHitCount(defendCardsInHand)
                .FromCard(this)
                .TargetingAllOpponents(base.CombatState)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
        }
    }
}
