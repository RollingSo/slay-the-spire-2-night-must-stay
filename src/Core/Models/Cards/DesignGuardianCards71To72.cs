using NightMustStay.Core.Nodes.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards
{
    // Card-table ID 71: 锋芒毕现
    public sealed class FinalCurtainHalberd : CardModel
    {
        private const string IncreaseKey = "Increase";
        private decimal _extraDamageFromWaiting;

        private decimal ExtraDamageFromWaiting
        {
            get => _extraDamageFromWaiting;
            set
            {
                AssertMutable();
                _extraDamageFromWaiting = value;
            }
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(10m, ValueProp.Move),
            new DynamicVar(IncreaseKey, 12m)
        };

        public FinalCurtainHalberd()
            : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithGuardianWeaponFx()
                .Execute(context);
        }

        public override Task AfterSideTurnEnd(
            PlayerChoiceContext context,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (side != Owner.Creature.Side || Pile == null || !Pile.Type.IsCombatPile())
                return Task.CompletedTask;

            bool playedAttack = CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
                entry.HappenedThisTurn(CombatState)
                && entry.CardPlay.Card.Owner == Owner
                && entry.CardPlay.Card.Type == CardType.Attack);
            if (!playedAttack)
            {
                decimal increase = DynamicVars[IncreaseKey].BaseValue;
                DynamicVars.Damage.BaseValue += increase;
                ExtraDamageFromWaiting += increase;
            }

            return Task.CompletedTask;
        }

        protected override void OnUpgrade() => DynamicVars[IncreaseKey].UpgradeValueBy(4m);

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Damage.BaseValue += ExtraDamageFromWaiting;
        }
    }

    // Card-table ID 72: 无畏
    public sealed class Fearless : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromCard<ShieldPoke>() };

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };

        public Fearless() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

        public static bool IsShieldPokeEmpowered(CardModel card) =>
            card?.CombatState != null && card.Owner != null
            && CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
                entry.HappenedThisTurn(card.CombatState)
                && entry.CardPlay.Card.Owner == card.Owner
                && entry.CardPlay.Card is Fearless);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}
