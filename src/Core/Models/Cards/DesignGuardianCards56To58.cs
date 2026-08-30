using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    // Card-table ID 56: 风暴足
    public sealed class StormKick : GuardianConcealedEdgeCard
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/storm_kick.png");

        private const string WeakKey = "Weak";
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        public StormKick() : base(3, CardRarity.Common) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<WeakPower>(
                context,
                CombatState.HittableEnemies,
                DynamicVars[WeakKey].BaseValue,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => DynamicVars[WeakKey].UpgradeValueBy(1m);
    }

    // Card-table ID 57: 狩猎巨人
    public sealed class GiantHunter : CardModel
    {
        private const string ImbalanceKey = "Imbalance";
        private const string HpThresholdKey = "HpThreshold";

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/giant_hunter.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<PhantomImbalancePower>(ImbalanceKey, 2m),
            new DynamicVar(HpThresholdKey, 50m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>()
        };

        public GiantHunter() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal amount = DynamicVars[ImbalanceKey].BaseValue;
            if (cardPlay.Target.CurrentHp < DynamicVars[HpThresholdKey].BaseValue)
                amount *= 2m;

            await PowerCmd.Apply<PhantomImbalancePower>(
                context,
                cardPlay.Target,
                amount,
                Owner.Creature,
                this);
            await PhantomImbalancePower.ResolveThreshold(context, cardPlay.Target);
        }

        protected override void OnUpgrade() => DynamicVars[HpThresholdKey].UpgradeValueBy(20m);
    }

    // Card-table ID 58: 盾牌冲击
    public sealed class ShieldImpact : CardModel
    {
        private const string CalculatedDamageKey = "CalculatedDamage";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(0m, ValueProp.Move),
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar(CalculatedDamageKey).WithMultiplier(
                (card, _) => card.Owner.Creature.GetPower<FortifyPower>()?.Amount ?? 0m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<FortifyPower>()
        };

        public ShieldImpact() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal damage = ((CalculatedVar)DynamicVars[CalculatedDamageKey]).Calculate(cardPlay.Target);

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue + damage)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}
