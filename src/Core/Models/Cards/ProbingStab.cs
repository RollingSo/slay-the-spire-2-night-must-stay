using NightMustStay.Core.Nodes.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class ProbingStab : CardModel
    {


        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(7m, ValueProp.Move),
            new CardsVar(1),
        };

        public ProbingStab()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithGuardianWeaponFx()
                .Execute(choiceContext);

            var matches = PileType.Draw.GetPile(Owner).Cards.Where(GuardianCardFilters.HasDefendInName).Take(DynamicVars.Cards.IntValue).ToArray();
            foreach (var card in matches)
            {
                await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
                await CardPileCmd.Draw(choiceContext, 1m, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(1m);
            base.DynamicVars.Cards.UpgradeValueBy(1m);
        }
    }
}
