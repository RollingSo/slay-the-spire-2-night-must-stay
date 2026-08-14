using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    // 重戟无锋
    public sealed class HeavyHalberd : GuardianConcealedEdgeCard
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/heavy_halberd.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(28m, ValueProp.Move)
        };

        public HeavyHalberd()
            : base(5, CardRarity.Common, CardType.Attack, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8m);
    }

    public sealed class Featherstep : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/featherstep.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CardsVar(2)
        };

        public Featherstep()
            : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<FeatherstepPower>(
                context,
                Owner.Creature,
                DynamicVars.Cards.BaseValue,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
    }

    public sealed class DustReturnSlash : CardModel
    {
        private const int StunnedDamageMultiplier = 3;

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/dust_return_slash.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.Static(StaticHoverTip.Stun)
        };

        public DustReturnSlash()
            : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal damage = DynamicVars.Damage.BaseValue
                * (cardPlay.Target.IsStunned ? StunnedDamageMultiplier : 1);
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
    }

    public sealed class EveOfCounterattack : CardModel
    {
        private const string TotalGuardCounterKey = "TotalGuardCounter";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/eve_of_counterattack.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(4m),
            new CalculatedVar(TotalGuardCounterKey).WithMultiplier(
                static (card, _) => CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
                    entry.CardPlay.Card.Owner == card.Owner
                    && entry.CardPlay.Card is GuardianConcealedEdgeCard))
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>(),
            GuardianCardHoverTips.ConcealedEdge
        };

        public EveOfCounterattack()
            : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            decimal totalGuardCounter =
                ((CalculatedVar)DynamicVars[TotalGuardCounterKey]).Calculate(null);
            await PowerCmd.Apply<GuardCounterPower>(
                context,
                Owner.Creature,
                totalGuardCounter,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.CalculationExtra.UpgradeValueBy(2m);
    }

    public sealed class HideAndSeekStab : GuardianConcealedEdgeCard
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/hide_and_seek_stab.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(15m, ValueProp.Move)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromCard<ShieldPoke>(IsUpgraded)
        };

        public HideAndSeekStab()
            : base(4, CardRarity.Uncommon, CardType.Attack, TargetType.AnyEnemy)
        {
        }

        protected override void AddExtraArgsToDescription(LocString description) =>
            description.Add(
                "GeneratedCard",
                ModelDb.Card<ShieldPoke>().Title + (IsUpgraded ? "+" : string.Empty));

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);

            CardModel shieldPoke = CombatState.CreateCard<ShieldPoke>(Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(shieldPoke);
            await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Hand, Owner);
        }

        protected override void OnUpgrade()
        {
        }
    }
}

namespace sts2mod.Core.Models.Power
{
    public sealed class FeatherstepPower : PowerModel
    {
        private sealed class Data
        {
            public bool TriggeredThisTurn;
        }

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData() => new Data();

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            Data data = GetInternalData<Data>();
            if (data.TriggeredThisTurn
                || cardPlay.Card.Owner.Creature != Owner
                || cardPlay.Card is not GuardianConcealedEdgeCard)
            {
                return;
            }

            data.TriggeredThisTurn = true;
            Flash();
            await CardPileCmd.Draw(context, Amount, Owner.Player);
        }

        public override Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side == Owner.Side)
                GetInternalData<Data>().TriggeredThisTurn = false;
            return Task.CompletedTask;
        }
    }
}
