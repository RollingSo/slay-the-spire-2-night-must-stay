using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    public sealed class SwallowReturnWind : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/swallow_return_wind.png");

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<GuardCounterPower>() };

        public SwallowReturnWind()
            : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<SwallowReturnWindPower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class Heavenfall : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/heavenfall.png");

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public Heavenfall()
            : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<HeavenfallPower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
    }

    public sealed class RetreatingDefense : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/retreating_defense.png");

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.Static(StaticHoverTip.Block) };

        public RetreatingDefense()
            : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<RetreatingDefensePower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
    }

    public sealed class SkySweepingGod : CardModel
    {
        private const string StrengthKey = "Strength";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/sky_sweeping_god.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new PowerVar<StrengthPower>(StrengthKey, 1m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<StrengthPower>()
        };

        public SkySweepingGod()
            : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal weak = cardPlay.Target.GetPower<WeakPower>()?.Amount ?? 0m;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);

            if (weak > 0m)
            {
                await PowerCmd.Apply<StrengthPower>(
                    context,
                    Owner.Creature,
                    weak * DynamicVars[StrengthKey].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}

namespace sts2mod.Core.Models.Power
{
    public sealed class SwallowReturnWindPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
    }

    public sealed class HeavenfallPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterBlockGained(
            Creature creature,
            decimal amount,
            ValueProp props,
            CardModel card)
        {
            if (creature != Owner || amount <= 0m)
                return;

            Flash();
            await PowerCmd.Apply<GuardCounterPower>(
                new BlockingPlayerChoiceContext(),
                Owner,
                amount * Amount,
                Owner,
                card);
        }

        public override async Task AfterSideTurnEnd(
            PlayerChoiceContext context,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (side == Owner.Side)
                await PowerCmd.Remove(this);
        }
    }

    public sealed class RetreatingDefensePower : PowerModel
    {
        private sealed class Data
        {
            public bool Triggered;
            public bool RetainApplied;
            public readonly List<CardModel> TemporarilyRetainedCards = new();
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData() => new Data();

        public void AfterFullyBlockedAttack()
        {
            GetInternalData<Data>().Triggered = true;
            Flash();
        }

        public override async Task BeforeFlush(
            PlayerChoiceContext context,
            Player player)
        {
            Data data = GetInternalData<Data>();
            if (player != Owner.Player || data.RetainApplied)
                return;

            data.TemporarilyRetainedCards.Clear();
            foreach (CardModel card in PileType.Hand.GetPile(player).Cards
                         .Where(card => !card.Keywords.Contains(CardKeyword.Retain)))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Retain);
                data.TemporarilyRetainedCards.Add(card);
            }

            data.RetainApplied = true;
            Flash();
            await Task.CompletedTask;
        }

        public override async Task BeforeHandDraw(
            Player player,
            PlayerChoiceContext context,
            ICombatState combatState)
        {
            Data data = GetInternalData<Data>();
            if (player != Owner.Player || !data.RetainApplied)
                return;

            foreach (CardModel card in data.TemporarilyRetainedCards)
                card.RemoveKeyword(CardKeyword.Retain);

            data.TemporarilyRetainedCards.Clear();
            data.RetainApplied = false;

            if (Amount <= 1m)
                await PowerCmd.Remove(this);
            else
                await PowerCmd.ModifyAmount(context, this, -1m, Applier, null);
        }

        public override async Task AfterSideTurnEnd(
            PlayerChoiceContext context,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (side != CombatSide.Enemy)
                return;

            Data data = GetInternalData<Data>();
            if (data.Triggered)
                return;

            CardPile hand = PileType.Hand.GetPile(Owner.Player);
            foreach (CardModel card in data.TemporarilyRetainedCards)
            {
                card.RemoveKeyword(CardKeyword.Retain);
                if (hand.Cards.Contains(card))
                    await CardPileCmd.Add(card, PileType.Discard);
            }

            await PowerCmd.Remove(this);
        }
    }
}
