using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Characters;
using sts2mod.Core.Models.Relics;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Power
{
    public interface ILongShotCard
    {
    }

    public interface IMarkTriggerObserver
    {
        void OnMarkTriggered(decimal triggeringDamage);
    }

    public interface IPoisonBurstTriggerPower
    {
        Task AfterPoisonBurstTriggered(
            PlayerChoiceContext choiceContext,
            Creature target,
            CardModel cardSource);
    }

    public sealed class DistancePower : PowerModel
    {
        private sealed class Data
        {
            public decimal LastResolvedDistance;
            public decimal DistanceMovedThisTurn;
        }

        public const int MinimumDistance = -5;
        public const int MaximumDistance = 5;

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override bool AllowNegative => true;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<DexterityPower>(),
            HoverTipFactory.FromPower<StrengthPower>(),
        };

        protected override object InitInternalData() => new Data();

        public decimal DistanceMovedThisTurn =>
            GetInternalData<Data>().DistanceMovedThisTurn;

        public override decimal GetScaledAmountForMultiplayer(
            ICombatState combatState,
            Creature applier,
            decimal amount,
            Creature target,
            CardModel cardSource)
        {
            decimal current = target.GetPower<DistancePower>()?.Amount ?? 0m;
            decimal clamped = decimal.Clamp(
                current + amount,
                MinimumDistance,
                MaximumDistance);
            decimal appliedAmount = clamped - current;

            if (cardSource != null
                && amount != 0m
                && appliedAmount == 0m
                && target.Player?.Character is Ironeye)
            {
                string message = amount > 0m
                    ? "距离已达最大限制。"
                    : "距离已达最小限制。";
                NThoughtBubbleVfx.Create(message, target, 1.6);
            }

            return appliedAmount;
        }

        public override Task AfterApplied(Creature applier, CardModel cardSource) =>
            SynchronizeVisibleStatChanges(
                new BlockingPlayerChoiceContext(),
                cardSource);

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature applier,
            CardModel cardSource)
        {
            if (power != this || amount == 0m)
                return;

            GetInternalData<Data>().DistanceMovedThisTurn += decimal.Abs(amount);
            await SynchronizeVisibleStatChanges(choiceContext, cardSource);
            RefreshDistanceDependentCardPreviews();
        }

        public override Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> participants,
            ICombatState combatState)
        {
            if (side == Owner.Side && participants.Contains(Owner))
            {
                GetInternalData<Data>().DistanceMovedThisTurn = 0m;
                RefreshDistanceDependentCardPreviews();
            }

            return Task.CompletedTask;
        }

        private static void RefreshDistanceDependentCardPreviews()
        {
            if (NCombatRoom.Instance?.Ui?.Hand == null)
                return;

            foreach (var holder in NCombatRoom.Instance.Ui.Hand.ActiveHolders)
                holder.UpdateCard();
        }

        private async Task SynchronizeVisibleStatChanges(
            PlayerChoiceContext choiceContext,
            CardModel cardSource)
        {
            Data data = GetInternalData<Data>();
            decimal newDistance = Amount;
            decimal oldDistance = data.LastResolvedDistance;
            if (newDistance == oldDistance)
                return;

            // Update before awaiting the visible power changes. Power application
            // dispatches global callbacks, so this also makes duplicate/re-entrant
            // Distance callbacks idempotent.
            data.LastResolvedDistance = newDistance;

            decimal dexterityDelta = newDistance - oldDistance;
            decimal strengthDelta =
                GetStrengthContribution(newDistance)
                - GetStrengthContribution(oldDistance);

            if (dexterityDelta != 0m)
            {
                await PowerCmd.Apply<DexterityPower>(
                    choiceContext,
                    Owner,
                    dexterityDelta,
                    Owner,
                    cardSource);
            }

            if (strengthDelta != 0m)
            {
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    Owner,
                    strengthDelta,
                    Owner,
                    cardSource);
            }
        }

        private decimal GetStrengthContribution(decimal distance)
        {
            if (distance > 0m)
                return Owner.HasPower<BowLikeFullMoonPower>() ? 0m : -distance;

            if (distance < 0m)
            {
                decimal multiplier =
                    Owner.HasPower<BladeShadowUnmatchedPower>() ? 2m : 1m;
                return -distance * multiplier;
            }

            return 0m;
        }

        public override bool TryModifyEnergyCostInCombat(
            CardModel card,
            decimal originalCost,
            out decimal modifiedCost)
        {
            modifiedCost = originalCost;
            if (card.Owner.Creature != Owner
                || card is not ILongShotCard
                || Amount < 2
                || originalCost <= 0m)
            {
                return false;
            }

            modifiedCost = originalCost - 1m;
            if (modifiedCost < 0m)
                modifiedCost = 0m;
            return true;
        }

    }

    public sealed class LongShotPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };
    }

    public sealed class PoisonBurstPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<HiddenPoisonPower>() };

        public static async Task Trigger(
            PlayerChoiceContext choiceContext,
            Creature target,
            Creature applier,
            CardModel cardSource)
        {
            HiddenPoisonPower hiddenPoison = target.GetPower<HiddenPoisonPower>();
            if (hiddenPoison == null || hiddenPoison.Amount <= 0m || !target.IsAlive)
                return;

            await hiddenPoison.Trigger(choiceContext);

            foreach (IPoisonBurstTriggerPower triggerPower in applier.Powers
                         .OfType<IPoisonBurstTriggerPower>()
                         .ToArray())
            {
                await triggerPower.AfterPoisonBurstTriggered(
                    choiceContext,
                    target,
                    cardSource);
            }
        }
    }

    public sealed class HiddenPoisonPower : PowerModel
    {
        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };

        public async Task Trigger(PlayerChoiceContext choiceContext)
        {
            if (!Owner.IsAlive || Amount <= 0)
                return;

            Flash();
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                Amount,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Applier,
                null);
        }

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> participants,
            ICombatState combatState)
        {
            if (side != Owner.Side || !participants.Contains(Owner))
                return;

            var context = new ThrowingPlayerChoiceContext();
            await Trigger(context);
        }
    }

    public sealed class LightningArrowheadPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<LongShotPower>(),
                HoverTipFactory.FromPower<DistancePower>(),
            };

        public override async Task AfterCardPlayed(
            PlayerChoiceContext context,
            CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner.Creature != Owner
                || cardPlay.Card is not ILongShotCard
                || (Owner.GetPower<DistancePower>()?.Amount ?? 0m) < 2m)
            {
                return;
            }

            Creature[] enemies = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();
            if (enemies.Length == 0)
                return;

            Creature target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
            Flash();
            NightreignHitVfx.PlayIroneyeMarkTrigger(target);
            await CreatureCmd.Damage(
                context,
                target,
                Amount,
                ValueProp.Unpowered,
                Owner,
                cardPlay.Card);
        }
    }

    public sealed class BowLikeFullMoonPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Single;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<StrengthPower>(),
            };

        public override async Task AfterApplied(Creature applier, CardModel cardSource)
        {
            decimal distance = Owner.GetPower<DistancePower>()?.Amount ?? 0m;
            if (distance > 0m)
            {
                await PowerCmd.Apply<StrengthPower>(
                    new BlockingPlayerChoiceContext(),
                    Owner,
                    distance,
                    Owner,
                    cardSource);
            }
        }
    }

    public sealed class BladeShadowUnmatchedPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Single;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<StrengthPower>(),
            };

        public override async Task AfterApplied(Creature applier, CardModel cardSource)
        {
            decimal distance = Owner.GetPower<DistancePower>()?.Amount ?? 0m;
            if (distance < 0m)
            {
                await PowerCmd.Apply<StrengthPower>(
                    new BlockingPlayerChoiceContext(),
                    Owner,
                    -distance,
                    Owner,
                    cardSource);
            }
        }
    }

    public sealed class NextTurnDistancePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != Owner.Side)
                return;

            Flash();
            await PowerCmd.Apply<DistancePower>(
                new BlockingPlayerChoiceContext(),
                Owner,
                Amount,
                Owner,
                null);
            await PowerCmd.Remove(this);
        }
    }

    public sealed class PoisonBladePower : PowerModel, IMarkTriggerPower
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<HiddenPoisonPower>() };

        public async Task AfterMarkTriggered(
            PlayerChoiceContext choiceContext,
            Creature markedTarget,
            CardModel triggeringCard)
        {
            if (!markedTarget.IsAlive || Amount <= 0m)
                return;

            Flash();
            await PowerCmd.Apply<HiddenPoisonPower>(
                choiceContext,
                markedTarget,
                Amount,
                Owner,
                triggeringCard);
        }
    }

    public sealed class EvasiveArrowSlashPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<DistancePower>() };

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature applier,
            CardModel cardSource)
        {
            if (power is not DistancePower
                || power.Owner != Owner
                || amount == 0m)
            {
                return;
            }

            int triggerCount = decimal.ToInt32(decimal.Abs(amount));
            Flash();
            for (int i = 0; i < triggerCount; i++)
            {
                Creature[] enemies = CombatState.HittableEnemies
                    .Where(enemy => enemy.IsAlive)
                    .ToArray();
                if (enemies.Length == 0)
                    return;

                Creature target =
                    enemies[0].Monster?.Rng.NextItem(enemies) ?? enemies[0];
                NightreignHitVfx.PlayIroneyeKnife(target);
                await CreatureCmd.Damage(
                    choiceContext,
                    target,
                    Amount,
                    ValueProp.Unpowered,
                    Owner,
                    null);
            }
        }
    }

    public sealed class PierceTheWillowPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };
    }

    public sealed class DisorderlyArrowsPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromPower<MarkPower>() };

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature applier,
            CardModel cardSource)
        {
            if (power is not MarkPower
                || amount <= 0m
                || applier != Owner
                || !power.Owner.IsAlive)
            {
                return;
            }

            Flash();
            NightreignHitVfx.PlayIroneyeMarkTrigger(power.Owner);
            await CreatureCmd.Damage(
                choiceContext,
                power.Owner,
                Amount,
                ValueProp.Unpowered,
                Owner,
                null);
        }
    }

    public sealed class EagleEyePower : PowerModel
    {
        private const string ThresholdKey = "Threshold";

        private sealed class Data
        {
            public decimal DistanceMoved;
            public decimal Threshold = 4m;
        }

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override int DisplayAmount
        {
            get
            {
                decimal threshold = GetInternalData<Data>().Threshold;
                if (threshold <= 0m)
                    return 0;

                decimal progress = GetInternalData<Data>().DistanceMoved % threshold;
                return decimal.ToInt32(threshold - progress);
            }
        }

        public override PowerInstanceType InstanceType =>
            PowerInstanceType.Instanced;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new DynamicVar(ThresholdKey, 4m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<MarkPower>(),
            };

        protected override object InitInternalData() => new Data();

        public void SetThreshold(decimal threshold)
        {
            if (threshold <= 0m)
                return;

            GetInternalData<Data>().Threshold = threshold;
            DynamicVars[ThresholdKey].BaseValue = threshold;
            InvokeDisplayAmountChanged();
        }

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature applier,
            CardModel cardSource)
        {
            if (power is not DistancePower
                || power.Owner != Owner
                || amount == 0m)
            {
                return;
            }

            Data data = GetInternalData<Data>();
            decimal threshold = data.Threshold;
            if (threshold <= 0m)
                return;

            data.DistanceMoved += decimal.Abs(amount);
            while (data.DistanceMoved >= threshold)
            {
                data.DistanceMoved -= threshold;
                Creature[] enemies = CombatState.HittableEnemies
                    .Where(enemy => enemy.IsAlive)
                    .ToArray();
                if (enemies.Length == 0)
                {
                    InvokeDisplayAmountChanged();
                    return;
                }

                Creature target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
                Flash();
                await PowerCmd.Apply<MarkPower>(
                    choiceContext,
                    target,
                    Amount,
                    Owner,
                    null);
            }

            InvokeDisplayAmountChanged();
        }
    }

    public sealed class MarkPower : PowerModel
    {
        private const int TriggerInterval = 5;
        private const int BonusDamage = 10;

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override Task AfterApplied(Creature applier, CardModel cardSource)
        {
            IroneyeMarkStatusVfx.Ensure(Owner);
            return Task.CompletedTask;
        }

        public override Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature applier,
            CardModel cardSource)
        {
            if (power == this && Amount > 0)
                IroneyeMarkStatusVfx.Ensure(Owner);
            return Task.CompletedTask;
        }

        public override Task AfterRemoved(Creature remover)
        {
            IroneyeMarkStatusVfx.Remove(Owner);
            return Task.CompletedTask;
        }

        public override async Task AfterDamageReceived(
            PlayerChoiceContext choiceContext,
            Creature target,
            DamageResult result,
            ValueProp props,
            Creature dealer,
            CardModel cardSource)
        {
            decimal expandedRange =
                dealer?.GetPower<PierceTheWillowPower>()?.Amount ?? 0m;
            expandedRange += dealer?.Player?.GetRelic<SacredRhythmBlade>()
                ?.TriggerRangeIncrease ?? 0m;
            decimal remainder = result.TotalDamage % TriggerInterval;
            decimal distanceToMultiple = decimal.Min(
                decimal.Abs(remainder),
                TriggerInterval - decimal.Abs(remainder));
            bool standardTrigger = result.TotalDamage > 0m
                && distanceToMultiple <= expandedRange;
            FatalBladeEdgePower fatalBlade = dealer?.GetPower<FatalBladeEdgePower>();
            decimal distance = dealer?.GetPower<DistancePower>()?.Amount ?? 0m;
            bool fatalBladeTrigger = result.TotalDamage > 0m
                && fatalBlade != null
                && distance <= -fatalBlade.Amount;
            if (target != Owner
                || dealer == null
                || dealer.Side != CombatSide.Player
                || cardSource?.Type != CardType.Attack
                || !props.HasFlag(ValueProp.Move)
                || (!standardTrigger && !fatalBladeTrigger))
            {
                return;
            }

            await TriggerOne(choiceContext, dealer, cardSource, result.TotalDamage);
        }

        public async Task TriggerOne(
            PlayerChoiceContext choiceContext,
            Creature dealer,
            CardModel cardSource,
            decimal triggeringDamage = 0m)
        {
            if (Amount <= 0m || !Owner.IsAlive || dealer == null)
                return;

            Creature applier = Applier ?? dealer;
            Flash();
            if (cardSource is IMarkTriggerObserver observer)
                observer.OnMarkTriggered(triggeringDamage);

            await PowerCmd.Decrement(this);

            foreach (IMarkTriggerPower triggerPower in dealer.Powers
                         .OfType<IMarkTriggerPower>()
                         .ToArray())
            {
                await triggerPower.AfterMarkTriggered(
                    choiceContext,
                    Owner,
                    cardSource);
            }

            ProtectiveScaleArmor scaleArmor =
                dealer.Player?.GetRelic<ProtectiveScaleArmor>();
            if (scaleArmor != null)
                await scaleArmor.AfterMarkTriggered(choiceContext);

            if (!Owner.IsAlive)
                return;

            IroneyeMarkStatusVfx.Pulse(Owner);
            NightreignHitVfx.PlayIroneyeMarkTrigger(Owner);
            decimal hiddenPoison = Owner.GetPower<HiddenPoisonPower>()?.Amount ?? 0m;
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                BonusDamage + hiddenPoison,
                ValueProp.Unpowered,
                applier,
                null);
        }

        public async Task TriggerAll(
            PlayerChoiceContext choiceContext,
            Creature dealer,
            CardModel cardSource)
        {
            int triggerCount = (int)Amount;
            for (int i = 0; i < triggerCount && Owner.IsAlive; i++)
                await TriggerOne(choiceContext, dealer, cardSource);
        }
    }
}
