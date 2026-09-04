using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class IroneyeShadowAssault : CardModel
    {
        private const string DistanceKey = "Distance";

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar(DistanceKey, 1m),
            new DamageVar(9m, ValueProp.Move),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
            HoverTipFactory.FromPower<DistancePower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/shadow_assault.png");

        public IroneyeShadowAssault()
            : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
                .Execute(context);
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(3m);
    }

    public sealed class IroneyeHeadshot : CardModel, IMarkTriggerObserver
    {
        private bool _triggeredMark;

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DamageVar(10m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Retain),
            HoverTipFactory.FromPower<NightMustStayMarkPower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/headshot.png");

        public IroneyeHeadshot()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        public void OnMarkTriggered(decimal triggeringDamage)
        {
            _triggeredMark = true;
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            _triggeredMark = false;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);

            if (_triggeredMark
                && cardPlay.Target.IsAlive
                && cardPlay.Target.GetPower<NightMustStayMarkPower>() is { } remainingMark)
                await remainingMark.TriggerAll(context, Owner.Creature, this);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class MisdirectionStep : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            HoverTipFactory.FromCardWithCardHoverTips<Approach>()
                .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>())
                .Concat(new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) });

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/misdirection_step.png");

        public MisdirectionStep()
            : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            CardModel approach = CombatState.CreateCard<Approach>(Owner);
            CardModel retreat = CombatState.CreateCard<Retreat>(Owner);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    approach,
                    PileType.Hand,
                    Owner));
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    retreat,
                    PileType.Hand,
                    Owner));
        }

        protected override void OnUpgrade() =>
            CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
    }

    public sealed class IroneyeArrowRain : CardModel, ILongShotCard
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(7m, ValueProp.Move),
            new RepeatVar(2),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<LongShotPower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/arrow_rain.png");

        public IroneyeArrowRain()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .CompatFromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    public sealed class IroneyePoisonArrow : CardModel, ILongShotCard
    {
        private const string HiddenPoisonKey = "HiddenPoison";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(7m, ValueProp.Move),
                new PowerVar<HiddenPoisonPower>(HiddenPoisonKey, 3m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<LongShotPower>(),
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<HiddenPoisonPower>(),
                EnergyHoverTip,
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/poison_arrow.png");

        public IroneyePoisonArrow()
            : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);

            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<HiddenPoisonPower>(
                    context,
                    cardPlay.Target,
                    DynamicVars[HiddenPoisonKey].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars[HiddenPoisonKey].UpgradeValueBy(2m);
    }
}
