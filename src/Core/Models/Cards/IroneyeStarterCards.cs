using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class IroneyeMark : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string MarkKey = "Mark";

        public override bool GainsBlock => true;

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/mark.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(4m, ValueProp.Move),
            new DynamicVar(DistanceKey, 1m),
            new PowerVar<NightMustStayMarkPower>(MarkKey, 1m),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<NightMustStayMarkPower>(),
        };

        public IroneyeMark()
            : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

            await PowerCmd.Apply<DistancePower>(
                choiceContext,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);

            NightreignHitVfx.PlayIroneyeKnife(cardPlay.Target);
            await PowerCmd.Apply<NightMustStayMarkPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars[MarkKey].UpgradeValueBy(1m);
        }
    }

    public sealed class FullDraw : CardModel, ILongShotCard
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/full_draw.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(17m, ValueProp.Move),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<LongShotPower>(),
            EnergyHoverTip,
        };

        public FullDraw()
            : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(choiceContext);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
    }

    public sealed class Approach : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string CardsKey = "Cards";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/approach.png");

        public override CardPoolModel Pool =>
            ModelDb.CardPool<TokenCardPool>();

        public override CardPoolModel VisualCardPool =>
            ModelDb.CardPool<ColorlessCardPool>();

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain, CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 1m),
                new DamageVar(6m, ValueProp.Move),
                new DynamicVar(CardsKey, 0m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromKeyword(CardKeyword.Retain),
                HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
                HoverTipFactory.FromPower<DistancePower>(),
            };

        public Approach()
            : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
                .Execute(choiceContext);

            await PowerCmd.Apply<DistancePower>(
                choiceContext,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);

            if (DynamicVars[CardsKey].BaseValue > 0m)
            {
                await CardPileCmd.Draw(
                    choiceContext,
                    DynamicVars[CardsKey].BaseValue,
                    Owner);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars[CardsKey].UpgradeValueBy(1m);
    }

    public sealed class Retreat : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string CardsKey = "Cards";

        public override bool GainsBlock => true;

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/retreat.png");

        public override CardPoolModel Pool =>
            ModelDb.CardPool<TokenCardPool>();

        public override CardPoolModel VisualCardPool =>
            ModelDb.CardPool<ColorlessCardPool>();

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain, CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 1m),
                new BlockVar(5m, ValueProp.Move),
                new DynamicVar(CardsKey, 0m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromKeyword(CardKeyword.Retain),
                HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.Static(StaticHoverTip.Block),
            };

        public Retreat()
            : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars.Block,
                cardPlay);

            await PowerCmd.Apply<DistancePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);

            if (DynamicVars[CardsKey].BaseValue > 0m)
            {
                await CardPileCmd.Draw(
                    choiceContext,
                    DynamicVars[CardsKey].BaseValue,
                    Owner);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars[CardsKey].UpgradeValueBy(1m);
    }
}
