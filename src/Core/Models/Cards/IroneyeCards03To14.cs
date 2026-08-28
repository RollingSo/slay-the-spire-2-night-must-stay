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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards
{
    internal static class IroneyeHiddenPoison
    {
        public static Task BurstOnce(
            PlayerChoiceContext context,
            Creature target,
            Creature applier,
            CardModel cardSource) =>
            PoisonBurstPower.Trigger(context, target, applier, cardSource);

        public static decimal ResolveDamage(
            Creature target,
            decimal baseDamage,
            decimal requiredHiddenPoison)
        {
            decimal hiddenPoison = target.GetPower<HiddenPoisonPower>()?.Amount ?? 0m;
            return hiddenPoison >= requiredHiddenPoison
                ? baseDamage * 2m
                : baseDamage;
        }
    }

    // Card-table ID 3: 连续射击
    public sealed class ContinuousShooting : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(3m, ValueProp.Move),
            new RepeatVar(1),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/continuous_shooting.png");

        public ContinuousShooting()
            : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);

            DynamicVars.Repeat.BaseValue += 1m;
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // Card-table ID 4: 淬毒匕首
    public sealed class VenomDagger : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(3m, ValueProp.Move),
            new PowerVar<HiddenPoisonPower>("HiddenPoison", 2m),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/venom_dagger.png");

        public VenomDagger()
            : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
                .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
                .Execute(context);

            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<HiddenPoisonPower>(
                    context,
                    cardPlay.Target,
                    DynamicVars["HiddenPoison"].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars["HiddenPoison"].UpgradeValueBy(1m);
    }

    // Card-table ID 5: 后跃射击
    public sealed class BackstepShot : CardModel
    {
        private const string DistanceKey = "Distance";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new DynamicVar(DistanceKey, 1m),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/backstep_shot.png");

        public BackstepShot()
            : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
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

            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(3m);
    }

    // Card-table ID 6: 响指
    public sealed class FingerSnap : CardModel
    {
        private const string CardsKey = "Cards";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DynamicVar(CardsKey, 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<PoisonBurstPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/finger_snap.png");

        public FingerSnap()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await CardPileCmd.Draw(context, DynamicVars[CardsKey].IntValue, Owner);

            foreach (Creature enemy in CombatState.HittableEnemies
                         .Where(enemy => enemy.IsAlive)
                         .ToArray())
            {
                await IroneyeHiddenPoison.BurstOnce(
                    context,
                    enemy,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars[CardsKey].UpgradeValueBy(1m);
    }

    // Card-table ID 7: 毒爆
    public sealed class PoisonBurst : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(4m, ValueProp.Move),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<PoisonBurstPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/poison_burst.png");

        public PoisonBurst()
            : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateIroneyeMarkTrigger)
                .Execute(context);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // Card-table ID 8: 双吻毒蛾
    public sealed class TwinKissPoisonMoth : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(8m, ValueProp.Move),
            new RepeatVar(2),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<PoisonBurstPower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/twin_kiss_poison_moth.png");

        public TwinKissPoisonMoth()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            for (int i = 0; i < DynamicVars.Repeat.IntValue && cardPlay.Target.IsAlive; i++)
            {
                await IroneyeHiddenPoison.BurstOnce(
                    context,
                    cardPlay.Target,
                    Owner.Creature,
                    this);
            }

            if (!cardPlay.Target.IsAlive)
                return;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateIroneyeKnife)
                .Execute(context);

        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 9: 对空射击
    public sealed class AntiAirShot : CardModel, ILongShotCard
    {
        private const string MarkKey = "Mark";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(14m, ValueProp.Move),
            new PowerVar<MarkPower>(MarkKey, 1m),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<LongShotPower>(),
            HoverTipFactory.FromPower<MarkPower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/anti_air_shot.png");

        public AntiAirShot()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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
            DynamicVars.Damage.UpgradeValueBy(4m);
    }

    // Card-table ID 10: 宿灵射击
    public sealed class SpiritShot : CardModel, ILongShotCard
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DamageVar(12m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<LongShotPower>(),
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<MarkPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/spirit_shot.png");

        public SpiritShot()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            bool triggeredLongShot =
                (Owner.Creature.GetPower<DistancePower>()?.Amount ?? 0m) >= 2m;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);

            if (triggeredLongShot && cardPlay.Target.IsAlive)
            {
                MarkPower mark = cardPlay.Target.GetPower<MarkPower>();
                if (mark != null)
                    await mark.TriggerOne(context, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(4m);
    }

    // Card-table ID 11: 三箭齐射
    public sealed class TripleVolley : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(3m, ValueProp.Move),
            new RepeatVar(3),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/triple_volley.png");

        public TripleVolley()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
                .Execute(context);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // Card-table ID 13: 贴地滑移
    public sealed class GroundSkid : CardModel
    {
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(4m, ValueProp.Move),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
        }.Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>(IsUpgraded));

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/ground_skid.png");

        public GroundSkid()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            CardModel retreat = CombatState.CreateCard<Retreat>(Owner);
            if (IsUpgraded)
                CardCmd.Upgrade(retreat);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    retreat,
                    PileType.Hand,
                    Owner));
        }

        protected override void OnUpgrade()
        {
        }
    }

    // Card-table ID 14: 猎步标记
    public sealed class HunterStepMark : CardModel
    {
        private const string DistanceKey = "Distance";
        private const string MarkKey = "Mark";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(6m, ValueProp.Move),
            new DynamicVar(DistanceKey, 1m),
            new PowerVar<MarkPower>(MarkKey, 1m),
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<MarkPower>(),
        };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/hunter_step_mark.png");

        public HunterStepMark()
            : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            Creature[] targets = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();
            await PowerCmd.Apply<MarkPower>(
                context,
                targets,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
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
            DynamicVars[MarkKey].UpgradeValueBy(1m);
    }
}
