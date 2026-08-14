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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards
{
    public sealed class CloudRendingSweep : GuardianConcealedEdgeCard
    {
        private const string WeakKey = "Weak";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/cloud_rending_sweep.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(12m, ValueProp.Move),
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<StrengthPower>()
        };

        public CloudRendingSweep()
            : base(4, CardRarity.Rare, CardType.Attack, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(context);

            Creature[] targets = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            await PowerCmd.Apply<WeakPower>(
                context,
                targets,
                DynamicVars[WeakKey].BaseValue,
                Owner.Creature,
                this);

            foreach (Creature enemy in targets.Where(enemy => enemy.IsAlive))
            {
                decimal weak = enemy.GetPower<WeakPower>()?.Amount ?? 0m;
                if (weak <= 0m)
                    continue;

                await PowerCmd.Apply<CloudRendingStrengthRestorePower>(
                    context,
                    enemy,
                    weak,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars[WeakKey].UpgradeValueBy(1m);
        }
    }

    public sealed class CirclingGust : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/circling_gust.png");

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new CalculationBaseVar(0m),
            new CalculationExtraVar(3m),
            new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
                static (_, target) => target?.GetPower<WeakPower>()?.Amount ?? 0m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        };

        public CirclingGust()
            : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal block = DynamicVars.CalculatedBlock.Calculate(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(context);

            if (block > 0m)
            {
                await CreatureCmd.GainBlock(
                    Owner.Creature,
                    block,
                    DynamicVars.CalculatedBlock.Props,
                    cardPlay);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.CalculationExtra.UpgradeValueBy(1m);
        }
    }

    public sealed class WorldEndingWings : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/world_ending_wings.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CalculationBaseVar(20m),
            new ExtraDamageVar(4m),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                static (card, _) => PileType.Discard.GetPile(card.Owner).Cards.Count(
                    discardCard => discardCard.Type == CardType.Skill))
        };

        public WorldEndingWings()
            : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            decimal damage = DynamicVars.CalculatedDamage.Calculate(null);
            CardModel[] skills = PileType.Discard.GetPile(Owner).Cards
                .Where(card => card.Type == CardType.Skill)
                .ToArray();
            foreach (CardModel skill in skills)
                await CardPileCmd.Add(skill, PileType.Exhaust);

            await DamageCmd.Attack(damage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(context);
        }

        protected override void OnUpgrade() =>
            DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}

namespace sts2mod.Core.Models.Power
{
    // Match Dying Star's native temporary Strength-loss lifecycle: the loss is
    // applied with this power and restored when the affected enemy's turn ends.
    public sealed class CloudRendingStrengthRestorePower : TemporaryStrengthPower
    {
        public override AbstractModel OriginModel =>
            ModelDb.Card<sts2mod.Core.Models.Cards.CloudRendingSweep>();

        protected override bool IsPositive => false;
    }
}
