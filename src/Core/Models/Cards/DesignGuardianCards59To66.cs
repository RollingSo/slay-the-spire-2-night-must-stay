using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    // Card-table ID 59: 风暴障壁
    public sealed class StormBarrier : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/storm_barrier.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(6m, ValueProp.Move),
            new PowerVar<WeakPower>("Weak", 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        public override bool GainsBlock => true;

        public StormBarrier() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<StormBarrierPower>(context, Owner.Creature, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars["Weak"].UpgradeValueBy(1m);
        }
    }

    // Card-table ID 60: 刃风汇聚
    public sealed class BladewindConvergence : CardModel
    {
        private const string TotalGuardCounterKey = "TotalGuardCounter";

        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/bladewind_convergence.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<WeakPower>("Weak", 1m),
            new CalculationBaseVar(0m),
            new CalculationExtraVar(3m),
            new CalculatedVar(TotalGuardCounterKey).WithMultiplier(
                static (card, _) => card.CombatState.HittableEnemies
                    .Where(enemy => enemy.IsAlive)
                    .Sum(enemy =>
                        (enemy.GetPower<WeakPower>()?.Amount ?? 0m)
                        + card.DynamicVars["Weak"].BaseValue))
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public BladewindConvergence() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            decimal totalGuardCounter =
                ((CalculatedVar)DynamicVars[TotalGuardCounterKey]).Calculate(null);
            await PowerCmd.Apply<WeakPower>(context, CombatState.HittableEnemies, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
            if (totalGuardCounter > 0m)
            {
                await PowerCmd.Apply<GuardCounterPower>(context, Owner.Creature,
                    totalGuardCounter,
                    Owner.Creature, this);
            }
        }

        protected override void OnUpgrade() => DynamicVars.CalculationExtra.UpgradeValueBy(1m);
    }

    // Card-table ID 61: 磨枪
    public sealed class SpearGrinding : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/spear_grinding.png");

        public SpearGrinding() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<SpearGrindingPower>(context, Owner.Creature, 1m, Owner.Creature, this);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 62: 进化之翼
    public sealed class EvolutionWings : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/evolution_wings.png");

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DexterityPower>(),
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<DexterityPower>(1m)
        };

        public EvolutionWings() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<EvolutionWingsPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        }

        protected override void OnUpgrade() => DynamicVars.Dexterity.UpgradeValueBy(1m);
    }

    // Card-table ID 63: 绝命一击
    public sealed class DesperateBlow : CardModel
    {
        private const string GuardCounterKey = "GuardCounter";

        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/desperate_blow.png");

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<GuardCounterPower>(GuardCounterKey, 4m)
        };

        public DesperateBlow() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            GuardCounterPower guardCounter = await PowerCmd.Apply<GuardCounterPower>(
                context,
                Owner.Creature,
                DynamicVars[GuardCounterKey].BaseValue,
                Owner.Creature,
                this);

            if (guardCounter != null && guardCounter.Amount > 0m)
            {
                await PowerCmd.ModifyAmount(
                    context,
                    guardCounter,
                    guardCounter.Amount,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
    }

    // Card-table ID 64: 坚盾
    public sealed class StalwartShield : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
            IsUpgraded ? new[] { CardKeyword.Innate } : Array.Empty<CardKeyword>();

        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/stalwart_shield.png");

        public StalwartShield() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<StalwartShieldPower>(context, Owner.Creature, 1m, Owner.Creature, this);

        protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
    }

    // Card-table ID 65: 山止（由达芙的先古魔典给予并升级）
    public sealed class CounterLikeTide : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/counter_like_tide.png");

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>(),
            HoverTipFactory.FromPower<FortifyPower>()
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar("StrengthLoss", 3m),
            new DynamicVar("GuardCounter", 12m),
            new DynamicVar("Fortify", 4m)
        };

        public CounterLikeTide() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PowerCmd.Apply<StrengthPower>(context, Owner.Creature, -DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<CounterLikeTidePower>(context, Owner.Creature, 1m, Owner.Creature, this);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 66: 不破架势（由欧洛巴斯的古老牙齿将踏地架势转化）
    public sealed class UnbreakableStance : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/unbreakable_stance.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar("ExtraPlays", 2m),
            new DynamicVar("GuardCounter", 8m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public UnbreakableStance() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PowerCmd.Apply<UnbreakableStancePower>(context, Owner.Creature, DynamicVars["ExtraPlays"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<GuardCounterPower>(context, Owner.Creature, DynamicVars["GuardCounter"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
            DynamicVars["GuardCounter"].UpgradeValueBy(4m);
        }
    }
}
