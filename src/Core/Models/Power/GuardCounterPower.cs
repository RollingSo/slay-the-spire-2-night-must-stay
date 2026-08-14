using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Relics;
using sts2mod.Core.Nodes.Vfx;
using sts2mod.Core.Patches;

namespace sts2mod.Core.Models.Power
{
    public sealed class GuardCounterPower : PowerModel
    {
        private const ValueProp CounterDamageProps = ValueProp.Unpowered;

        public static bool SucceededAtStartOfThisTurn(CardModel card)
        {
            return SucceededAtStartOfThisTurn(card.CombatState, card.Owner?.Creature);
        }

        public static bool SucceededAtStartOfThisTurn(ICombatState state, Creature owner)
        {
            if (state == null || owner == null || state.CurrentSide != CombatSide.Player)
            {
                return false;
            }

            return _successfulTriggers.Any(trigger => trigger.Owner == owner
                && ReferenceEquals(trigger.CombatState, state)
                && trigger.RoundNumber == state.RoundNumber
                && trigger.Side == CombatSide.Player);
        }

        private readonly struct SuccessfulTrigger
        {
            public readonly Creature Owner;
            public readonly ICombatState CombatState;
            public readonly int RoundNumber;
            public readonly CombatSide Side;

            public SuccessfulTrigger(Creature owner, ICombatState combatState, int roundNumber, CombatSide side)
            {
                Owner = owner;
                CombatState = combatState;
                RoundNumber = roundNumber;
                Side = side;
            }
        }

        private static readonly List<SuccessfulTrigger> _successfulTriggers = new List<SuccessfulTrigger>();

        private sealed class Data
        {
            public CombatSide? RemovalSide;
        }

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public int CalculatePreviewTriggerCount(Creature attacker)
        {
            if (attacker?.Monster == null
                || !attacker.IsAlive
                || base.Owner == null
                || !base.Owner.IsAlive
                || base.Owner.Block < 0
                || base.CombatState == null)
            {
                return 0;
            }

            // AttackIntent calculates its damage against the local player. Only
            // preview this client's Guardian so multiplayer does not display a
            // counter for an attack whose remote target cannot be known yet.
            if (LocalContext.GetMe(base.CombatState)?.Creature != base.Owner)
            {
                return 0;
            }

            int remainingBlock = base.Owner.Block;
            foreach (Creature actingEnemy in base.CombatState.GetCreaturesOnSide(CombatSide.Enemy))
            {
                if (actingEnemy?.Monster == null || !actingEnemy.IsAlive)
                {
                    continue;
                }

                int poisonDamage = actingEnemy.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
                if (poisonDamage >= actingEnemy.CurrentHp)
                {
                    if (actingEnemy == attacker)
                    {
                        return 0;
                    }

                    continue;
                }

                int triggerCount = 0;
                foreach (AttackIntent intent in actingEnemy.Monster.NextMove.Intents.OfType<AttackIntent>())
                {
                    int incomingDamage = intent.GetTotalDamage(base.CombatState.PlayerCreatures, actingEnemy);
                    if (actingEnemy == attacker && incomingDamage <= remainingBlock)
                    {
                        triggerCount++;
                    }

                    remainingBlock = System.Math.Max(0, remainingBlock - incomingDamage);
                }

                if (actingEnemy == attacker)
                {
                    return triggerCount;
                }
            }

            return 0;
        }

        public int CalculatePreviewDamagePerTrigger(Creature attacker)
        {
            if (attacker == null || base.CombatState == null)
            {
                return 0;
            }

            decimal modifiedDamage = Hook.ModifyDamage(
                IRunState.GetFrom(new[] { base.Owner, attacker }),
                base.CombatState,
                attacker,
                base.Owner,
                base.Amount,
                CounterDamageProps,
                null,
                ModifyDamageHookType.All,
                CardPreviewMode.None,
                out IEnumerable<AbstractModel> _);
            return System.Math.Max(0, (int)modifiedDamage);
        }

        protected override object InitInternalData()
        {
            return new Data();
        }

        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side == base.Owner.Side)
            {
                ForgetOldTriggers(combatState);
            }

            return Task.CompletedTask;
        }

        public async Task ResolveAfterAttack(AttackCommand command)
        {
            Creature attacker = command.Attacker;
            if (attacker == null
                || attacker.Side != CombatSide.Enemy
                || !command.DamageProps.IsPoweredAttack())
            {
                return;
            }

            DamageResult[] resultsAgainstOwner = command.Results
                .SelectMany(hitResults => hitResults)
                .Where(result => result.Receiver == base.Owner)
                .ToArray();
            if (resultsAgainstOwner.Length == 0
                || resultsAgainstOwner.Any(result => result.UnblockedDamage > 0))
            {
                return;
            }

            await TriggerGuardCounter(new BlockingPlayerChoiceContext(), attacker);
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (GetInternalData<Data>().RemovalSide != side)
            {
                return;
            }

            if (base.Owner.HasPower<SwallowReturnWindPower>())
            {
                GetInternalData<Data>().RemovalSide = null;
                return;
            }

            await PowerCmd.Remove(this);
        }

        private async Task TriggerGuardCounter(PlayerChoiceContext choiceContext, Creature attacker)
        {
            GetInternalData<Data>().RemovalSide ??= base.CombatState.CurrentSide;
            GuardianAnimationPatch.PlayGuardCounter(base.Owner);
            Flash();
            if (attacker.IsAlive)
            {
                NightreignHitVfx.PlayGuardianCounter(attacker);
                await CreatureCmd.Damage(choiceContext, attacker, base.Amount, CounterDamageProps, base.Owner, null);
                EvolutionWingsPower evolutionWings = base.Owner.GetPower<EvolutionWingsPower>();
                if (evolutionWings != null)
                    await evolutionWings.AfterGuardCounterSucceeded(choiceContext);
                ThousandWeightHalberdPower halberd = base.Owner.GetPower<ThousandWeightHalberdPower>();
                if (halberd != null && attacker.IsAlive)
                    halberd.QueueImbalance(attacker);
            }

            RememberSuccessfulTrigger();
            foreach (StepForwardPursuit pursuit in base.Owner.Player.PlayerCombatState.AllCards.OfType<StepForwardPursuit>())
                pursuit.EnergyCost.SetUntilPlayed(0);
            foreach (Horn horn in base.Owner.Player.PlayerCombatState.AllCards.OfType<Horn>())
                horn.EnergyCost.SetUntilPlayed(0);

            GreenTalisman greenTalisman = base.Owner.Player?.GetRelic<GreenTalisman>();
            if (greenTalisman != null)
            {
                await greenTalisman.AfterGuardCounterSucceeded(choiceContext);
            }
            StormAvatarPower stormAvatar = base.Owner.GetPower<StormAvatarPower>();
            if (stormAvatar != null && attacker.IsAlive)
            {
                await stormAvatar.AfterGuardCounterSucceeded(choiceContext, attacker);
            }

            GuardianCharge[] charges = base.Owner.Player.PlayerCombatState.AllCards
                .OfType<GuardianCharge>()
                .ToArray();
            foreach (GuardianCharge charge in charges)
                await charge.AfterGuardCounterSucceeded();
        }

        private void RememberSuccessfulTrigger()
        {
            if (base.CombatState == null)
                return;

            _successfulTriggers.RemoveAll(trigger => trigger.Owner == base.Owner
                && !ReferenceEquals(trigger.CombatState, base.CombatState));
            if (!_successfulTriggers.Any(trigger => trigger.Owner == base.Owner
                && ReferenceEquals(trigger.CombatState, base.CombatState)
                && trigger.RoundNumber == base.CombatState.RoundNumber + 1
                && trigger.Side == CombatSide.Player))
            {
                _successfulTriggers.Add(new SuccessfulTrigger(base.Owner, base.CombatState, base.CombatState.RoundNumber + 1, CombatSide.Player));
            }
            ForgetOldTriggers(base.CombatState);
        }

        private static void ForgetOldTriggers(ICombatState combatState)
        {
            if (combatState == null)
                return;

            _successfulTriggers.RemoveAll(trigger => ReferenceEquals(trigger.CombatState, combatState)
                && trigger.RoundNumber < combatState.RoundNumber - 1);
        }
    }
}
