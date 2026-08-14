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
using sts2mod.Core.Models.Power;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards
{
    public sealed class LightningArrowhead : CardModel
    {
        private const string DamageKey = "Damage";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new PowerVar<LightningArrowheadPower>(DamageKey, 4m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<LongShotPower>(),
                HoverTipFactory.FromPower<DistancePower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/lightning_arrowhead.png");

        public LightningArrowhead()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<LightningArrowheadPower>(
                context,
                Owner.Creature,
                DynamicVars[DamageKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars[DamageKey].UpgradeValueBy(2m);
    }

    public sealed class BowLikeFullMoon : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<StrengthPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/bow_like_full_moon.png");

        public BowLikeFullMoon()
            : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<BowLikeFullMoonPower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class BladeShadowUnmatched : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<StrengthPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/blade_shadow_unmatched.png");

        public BladeShadowUnmatched()
            : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<BladeShadowUnmatchedPower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class CirclingManeuver : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string MarkKey = "Mark";

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 1m),
                new BlockVar(4m, ValueProp.Move),
                new PowerVar<MarkPower>(MarkKey, 1m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.Static(StaticHoverTip.Block),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/circling_maneuver.png");

        public CirclingManeuver()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<MarkPower>(
                    context,
                    cardPlay.Target,
                    DynamicVars[MarkKey].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars.Block.UpgradeValueBy(3m);
    }

    public sealed class WaveringStep : CardModel
    {
        private const string DistanceKey = "Distance";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DynamicVar(DistanceKey, 2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/wavering_step.png");

        public WaveringStep()
            : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<NextTurnDistancePower>(
                context,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
    }

    public sealed class KillingIntentGaze : CardModel
    {
        private const string MarkKey = "Mark";
        private const string HiddenPoisonKey = "HiddenPoison";

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<MarkPower>(MarkKey, 1m),
                new PowerVar<HiddenPoisonPower>(HiddenPoisonKey, 2m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
                HoverTipFactory.FromPower<MarkPower>(),
                HoverTipFactory.FromPower<HiddenPoisonPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/killing_intent_gaze.png");

        public KillingIntentGaze()
            : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            Creature[] enemies = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();
            await PowerCmd.Apply<MarkPower>(
                context,
                enemies,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<HiddenPoisonPower>(
                context,
                enemies,
                DynamicVars[HiddenPoisonKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars[MarkKey].UpgradeValueBy(1m);
            DynamicVars[HiddenPoisonKey].UpgradeValueBy(2m);
        }
    }

    public sealed class ReturnToZero : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string EnergyKey = "Energy";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 1m),
                new EnergyVar(EnergyKey, 2),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                EnergyHoverTip,
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/return_to_zero.png");

        public ReturnToZero()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            decimal distanceBefore =
                Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m;
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
            decimal distanceAfter =
                Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m;
            if (distanceBefore != distanceAfter && distanceAfter == 0m)
                await PlayerCmd.GainEnergy(DynamicVars[EnergyKey].BaseValue, Owner);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    public sealed class RetreatStep : CardModel
    {
        private const string DistanceKey = "Distance";

        public override bool GainsBlock => true;

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Retain };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 2m),
                new BlockVar(14m, ValueProp.Move),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromKeyword(CardKeyword.Retain),
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.Static(StaticHoverTip.Block),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/retreat_step.png");

        public RetreatStep()
            : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Block.UpgradeValueBy(4m);
    }

    public sealed class WitheringSlash : CardModel
    {
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            Array.Empty<DynamicVar>();

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<PoisonBurstPower>(),
                HoverTipFactory.FromPower<HiddenPoisonPower>(),
                HoverTipFactory.Static(StaticHoverTip.Block),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/withering_slash.png");

        public WitheringSlash()
            : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await IroneyeHiddenPoison.BurstOnce(
                context,
                cardPlay.Target,
                Owner.Creature,
                this);

            decimal block = cardPlay.Target.GetPower<HiddenPoisonPower>()?.Amount ?? 0m;
            if (block > 0m)
            {
                await CreatureCmd.GainBlock(
                    Owner.Creature,
                    block,
                    ValueProp.Move,
                    cardPlay);
            }
        }

        protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
    }

    public sealed class PoisonMistArrowArray : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(5m, ValueProp.Move),
                new PowerVar<HiddenPoisonPower>("HiddenPoison", 2m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<HiddenPoisonPower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/poison_mist_arrow_array.png");

        public PoisonMistArrowArray()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            foreach (Creature target in CombatState.HittableEnemies
                         .Where(enemy => enemy.IsAlive)
                         .ToArray())
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(target)
                    .WithHitVfxNode(hit =>
                        NightreignHitVfx.CreateIroneyeShot(Owner.Creature, hit))
                    .Execute(context);
                if (target.IsAlive)
                {
                    await PowerCmd.Apply<HiddenPoisonPower>(
                        context,
                        target,
                        DynamicVars["HiddenPoison"].BaseValue,
                        Owner.Creature,
                        this);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2m);
            DynamicVars["HiddenPoison"].UpgradeValueBy(1m);
        }
    }

    public sealed class BowCombatArt : CardModel
    {
        private const string CardsKey = "Cards";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(5m, ValueProp.Move),
                new CardsVar(2),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/bow_combat_art.png");

        public BowCombatArt()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);
            if ((Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m) < 0m)
            {
                await CardPileCmd.Draw(context, DynamicVars[CardsKey].IntValue, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
        }
    }

    public sealed class BladeGlide : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new CalculationBaseVar(0m),
                new ExtraDamageVar(1m),
                new PostDistanceCalculatedDamageVar(
                    ValueProp.Move,
                    static (card, _) =>
                    {
                        DistancePower distance =
                            card.Owner.Creature.GetPower<DistancePower>();
                        return (distance?.DistanceMovedThisTurn ?? 0m)
                            + decimal.Abs(distance?.Amount ?? 0m);
                    },
                    static _ => 0m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/blade_glide.png");

        public BladeGlide()
            : base(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            decimal currentDistance =
                Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m;

            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                -currentDistance,
                Owner.Creature,
                this);

            decimal damage = DynamicVars.CalculatedDamage.Calculate(null);
            if (damage > 0m)
            {
                await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .TargetingAllOpponents(CombatState)
                    .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
                    .Execute(context);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    public sealed class StarPlucker : CardModel
    {
        private const string CardsKey = "Cards";

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DynamicVar(CardsKey, 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/star_plucker.png");

        public StarPlucker()
            : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            IEnumerable<CardModel> drawn = await CardPileCmd.Draw(
                context,
                DynamicVars[CardsKey].BaseValue,
                Owner);
            foreach (CardModel card in drawn)
            {
                if (card.Type == CardType.Attack)
                    card.SetToFreeThisTurn();
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars[CardsKey].UpgradeValueBy(1m);
    }

    public sealed class Scouting : CardModel
    {
        private const string MarkKey = "Mark";

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new BlockVar(12m, ValueProp.Move),
                new PowerVar<MarkPower>(MarkKey, 1m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.Static(StaticHoverTip.Block),
                HoverTipFactory.FromPower<MarkPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/scouting.png");

        public Scouting()
            : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            Creature[] targets = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();
            await PowerCmd.Apply<MarkPower>(
                context,
                targets,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars[MarkKey].UpgradeValueBy(1m);
    }

    public sealed class PoisonBlade : CardModel
    {
        private const string HiddenPoisonKey = "HiddenPoison";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new PowerVar<HiddenPoisonPower>(HiddenPoisonKey, 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<PoisonBladePower>(),
                HoverTipFactory.FromPower<HiddenPoisonPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/poison_blade.png");

        public PoisonBlade()
            : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<PoisonBladePower>(
                context,
                Owner.Creature,
                DynamicVars[HiddenPoisonKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars[HiddenPoisonKey].UpgradeValueBy(1m);
    }

    public sealed class Aim : CardModel
    {
        private const string MarkKey = "Mark";
        private const string CardsKey = "Cards";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<MarkPower>(MarkKey, 2m),
                new DynamicVar(CardsKey, 1m),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/aim.png");

        public Aim()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await PowerCmd.Apply<MarkPower>(
                context,
                cardPlay.Target,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
            await CardPileCmd.Draw(
                context,
                DynamicVars[CardsKey].BaseValue,
                Owner);
        }

        protected override void OnUpgrade() =>
            DynamicVars[CardsKey].UpgradeValueBy(1m);
    }
}
