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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power
{
    public sealed class StormBarrierPower : PowerModel
    {
        private sealed class Data
        {
            public readonly Dictionary<Creature, decimal> PendingWeak = new Dictionary<Creature, decimal>();
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData() => new Data();

        public Task AfterFullyBlockedAttack(PlayerChoiceContext context, Creature attacker)
        {
            if (!attacker.IsAlive)
                return Task.CompletedTask;

            Flash();
            Data data = GetInternalData<Data>();
            data.PendingWeak.TryGetValue(attacker, out decimal pendingAmount);
            data.PendingWeak[attacker] = pendingAmount + Amount;
            return Task.CompletedTask;
        }

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != Owner.Side)
                return;

            Data data = GetInternalData<Data>();
            foreach (KeyValuePair<Creature, decimal> pending in data.PendingWeak.ToArray())
            {
                if (pending.Key.IsAlive)
                    await PowerCmd.Apply<WeakPower>(new BlockingPlayerChoiceContext(), pending.Key, pending.Value, Owner, null);
            }
            data.PendingWeak.Clear();
            await PowerCmd.Remove(this);
        }
    }

    public sealed class SpearGrindingPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
    }

    public sealed class SpearPolishPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Inky>();

        public override Task AfterApplied(Creature applier, CardModel cardSource)
        {
            foreach (CardModel card in Owner.Player.PlayerCombatState.AllCards)
                EnchantShieldPoke(card);

            return Task.CompletedTask;
        }

        public override Task AfterCardEnteredCombat(CardModel card)
        {
            if (card.Owner == Owner.Player)
                EnchantShieldPoke(card);

            return Task.CompletedTask;
        }

        private static void EnchantShieldPoke(CardModel card)
        {
            if (card is not ShieldPoke || card.Enchantment is Inky)
                return;

            if (card.Enchantment != null)
                CardCmd.ClearEnchantment(card);

            CardCmd.Enchant<Inky>(card, 1m);
        }
    }

    public sealed class EvolutionWingsPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public async Task AfterGuardCounterSucceeded(PlayerChoiceContext context)
        {
            Flash();
            await PowerCmd.Apply<DexterityPower>(
                context,
                Owner,
                Amount,
                Owner,
                null);
        }
    }

    public sealed class StalwartShieldPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        // Kept for compatibility with the legacy guard-counter hook; this
        // power now only draws a Defend card at the start of the turn.
        public void AfterEnemyAttackResolved(bool fullyBlocked) { }

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != Owner.Side)
                return;

            CardModel defendCard = PileType.Draw.GetPile(Owner.Player).Cards
                .FirstOrDefault(GuardianCardFilters.HasDefendInName);
            if (defendCard != null)
            {
                Flash();
                await CardPileCmd.Add(defendCard, PileType.Hand);
            }
        }
    }

    public sealed class NextEnemyTurnDamageReductionPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public override async Task BeforeSideTurnStart(
            PlayerChoiceContext context,
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != CombatSide.Enemy) return;
            await PowerCmd.Apply<IncomingDamageReductionThisTurnPower>(context, Owner, Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }

    public sealed class CounterLikeTidePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side != Owner.Side) return;
            Flash();
            BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
            await PowerCmd.Apply<GuardCounterPower>(context, Owner, Amount * 12m, Owner, null);
            await PowerCmd.Apply<FortifyPower>(context, Owner, Amount * 4m, Owner, null);
        }
    }

    public sealed class UnbreakableStancePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
        {
            if (card.Owner.Creature == Owner && GuardianCardFilters.HasDefendInName(card))
                return playCount + Amount;
            return playCount;
        }

        public override async Task AfterModifyingCardPlayCount(CardModel card)
        {
            if (card.Owner.Creature == Owner && GuardianCardFilters.HasDefendInName(card))
            {
                Flash();
                await PowerCmd.Remove(this);
            }
        }
    }
}
