using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class HalberdWingCombo : CardModel
    {
        private const string WeakKey = "Weak";

        protected override bool ShouldGlowGoldInternal => WasLastPlayedCardDefendInName;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(7m, ValueProp.Move),
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        private bool WasLastPlayedCardDefendInName => CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.HappenedThisTurn(base.CombatState) && entry.CardPlay.Card.Owner == base.Owner)
            is CardPlayFinishedEntry entry && GuardianCardFilters.HasDefendInName(entry.CardPlay.Card);

        public HalberdWingCombo()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            bool shouldApplyWeak = WasLastPlayedCardDefendInName;

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            if (shouldApplyWeak)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, base.DynamicVars[WeakKey].BaseValue, base.Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(3m);
            base.DynamicVars[WeakKey].UpgradeValueBy(1m);
        }
    }
}
