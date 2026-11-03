using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class HoldTheLine : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        public HoldTheLine()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.RetainHandPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            var power = await PowerCmd.Apply<NightMustStay.Core.Models.Power.HoldTheLinePower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            if (power != null) power.GrantingPlay = cardPlay;
        }

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}
