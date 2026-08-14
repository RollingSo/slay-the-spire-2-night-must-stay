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
    // Card-table ID 21: 穿杨
    public sealed class PierceTheWillow : CardModel
    {
        private const string RangeKey = "Range";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new PowerVar<PierceTheWillowPower>(RangeKey, 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/pierce_the_willow.png");

        public PierceTheWillow()
            : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<PierceTheWillowPower>(
                context,
                Owner.Creature,
                DynamicVars[RangeKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 22: 穿心箭
    public sealed class HeartpiercingArrow : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(6m, ValueProp.Move),
                new ExtraDamageVar(3m),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/heartpiercing_arrow.png");

        public HeartpiercingArrow()
            : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            decimal damage = DynamicVars.Damage.BaseValue;
            Creature[] targets = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();

            foreach (Creature target in targets)
            {
                if (!target.IsAlive)
                    continue;

                AttackCommand attack = await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .Targeting(target)
                    .WithHitVfxNode(hitTarget =>
                        NightreignHitVfx.CreateIroneyeShot(Owner.Creature, hitTarget))
                    .Execute(context);

                bool dealtDamage = attack.Results
                    .SelectMany(resultSet => resultSet)
                    .Any(result => result.TotalDamage > 0m);
                if (dealtDamage)
                    damage += DynamicVars.ExtraDamage.BaseValue;
            }
        }

        protected override void OnUpgrade() =>
            DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    // Card-table ID 23: 乱箭
    public sealed class DisorderlyArrows : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DamageVar(4m, ValueProp.Unpowered) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/disorderly_arrows.png");

        public DisorderlyArrows()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<DisorderlyArrowsPower>(
                context,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // Card-table ID 24: 惊弓之鸟
    public sealed class StartledBird : CardModel
    {
        private const string DistanceKey = "Distance";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DynamicVar(DistanceKey, 1m),
                new DamageVar(4m, ValueProp.Move),
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<MarkPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/startled_bird.png");

        public StartledBird()
            : base(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            Creature[] markedTargets = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive && enemy.HasPower<MarkPower>())
                .ToArray();
            foreach (Creature target in markedTargets)
            {
                if (!target.IsAlive)
                    continue;

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(target)
                    .WithHitVfxNode(hitTarget =>
                        NightreignHitVfx.CreateIroneyeShot(Owner.Creature, hitTarget))
                    .Execute(context);
            }

            await PowerCmd.Apply<DistancePower>(
                context,
                Owner.Creature,
                -DynamicVars[DistanceKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars.Damage.UpgradeValueBy(2m);
    }

    // Card-table ID 25: 鹰眼
    public sealed class EagleEye : CardModel
    {
        private const string ThresholdKey = "Threshold";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DynamicVar(ThresholdKey, 4m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<MarkPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/ironeye/eagle_eye.png");

        public EagleEye()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            EagleEyePower power = await PowerCmd.Apply<EagleEyePower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
            power?.SetThreshold(DynamicVars[ThresholdKey].BaseValue);
        }

        protected override void OnUpgrade() =>
            DynamicVars[ThresholdKey].UpgradeValueBy(-1m);
    }
}
