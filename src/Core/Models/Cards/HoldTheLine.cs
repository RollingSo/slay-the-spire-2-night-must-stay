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
        private bool _pending;
        private CardPlay _grantingPlay;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
        public override decimal ModifyBlockMultiplicative(MegaCrit.Sts2.Core.Entities.Creatures.Creature target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay) =>
            _pending && target == Owner.Creature && cardSource?.Owner == Owner && cardPlay != _grantingPlay ? 2m : 1m;
        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
        {
            if (_pending && play.Card.Owner == Owner && play != _grantingPlay) _pending = false;
            return Task.CompletedTask;
        }

        public HoldTheLine()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.RetainHandPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            _grantingPlay = cardPlay;
            _pending = true;
        }

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}
