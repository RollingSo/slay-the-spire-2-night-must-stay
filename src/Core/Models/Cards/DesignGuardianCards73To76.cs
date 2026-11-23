using NightMustStay.Core.Nodes.Vfx;
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
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class SwallowReturnWind : CardModel
    {
        public override string PortraitPath =>
            "res://packed/card_portraits/guardian/swallow_return_wind.png";

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

    public sealed class Heavenfall : GuardianConcealedEdgeCard
    {
        public override string PortraitPath => "res://packed/card_portraits/guardian/heavenfall.png";
        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(100m, ValueProp.Move) };

        public Heavenfall() : base(10, CardRarity.Rare, CardType.Attack, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this)
                .Targeting(cardPlay.Target).WithGuardianWeaponFx().Execute(context);
        }

        protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
    }

    public sealed class RetreatingDefense : CardModel
    {
        public override string PortraitPath =>
            "res://packed/card_portraits/guardian/retreating_defense.png";

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(4m, ValueProp.Move), new EnergyVar("Discount", 1) };

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
                context, Owner.Creature, DynamicVars["Discount"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
    }

    public sealed class SkySweepingGod : CardModel
    {
        private const string StrengthKey = "Strength";

        public override string PortraitPath =>
            "res://packed/card_portraits/guardian/sky_sweeping_god.png";

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
            : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal weak = cardPlay.Target.GetPower<WeakPower>()?.Amount ?? 0m;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithGuardianWeaponFx()
                .Execute(context);

            if (weak > 0m && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<StrengthPower>(
                    context,
                    cardPlay.Target,
                    -weak * DynamicVars[StrengthKey].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}

namespace NightMustStay.Core.Models.Power
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
            GuardCounterPower counter = await PowerCmd.Apply<GuardCounterPower>(
                new BlockingPlayerChoiceContext(),
                Owner,
                amount * Amount,
                Owner,
                card);
            if (counter != null) counter.HeavenfallVisual = true;
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
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        private bool IsOwnerAttack(CardModel card) =>
            card.Type == CardType.Attack && card.Owner.Creature == Owner;

        public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
        {
            modifiedCost = originalCost;
            if (Amount <= 0 || !IsOwnerAttack(card) || originalCost <= 0m || card.EnergyCost.CostsX)
                return false;

            modifiedCost = Math.Max(0m, originalCost - 1m);
            return true;
        }

        public override async Task BeforeCardPlayed(CardPlay cardPlay)
        {
            if (Amount <= 0 || cardPlay.PlayIndex != 0 || !IsOwnerAttack(cardPlay.Card)) return;
            Flash();
            await PowerCmd.Decrement(this);
        }

        public override async Task AfterSideTurnEnd(
            PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == Owner.Side && participants.Contains(Owner))
                await PowerCmd.Remove(this);
        }
    }
}
