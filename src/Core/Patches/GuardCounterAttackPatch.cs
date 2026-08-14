using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using System.Linq;

namespace sts2mod.Core.Patches;

[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute), typeof(PlayerChoiceContext))]
public static class GuardCounterAttackPatch
{
    [HarmonyPostfix]
    public static void AfterAttack(ref Task<AttackCommand> __result)
    {
        __result = ResolveGuardCounters(__result);
    }

    private static async Task<AttackCommand> ResolveGuardCounters(Task<AttackCommand> attackTask)
    {
        AttackCommand command = await attackTask;
        Creature attacker = command.Attacker;
        if (attacker == null)
        {
            return command;
        }

        foreach (Creature playerCreature in attacker.CombatState.PlayerCreatures)
        {
            var resultsAgainstPlayer = command.Results.SelectMany(hitResults => hitResults)
                .Where(result => result.Receiver == playerCreature)
                .ToList();
            bool isEnemyAttackAgainstPlayer = attacker.Side == MegaCrit.Sts2.Core.Combat.CombatSide.Enemy
                && command.DamageProps.IsPoweredAttack()
                && resultsAgainstPlayer.Count > 0;
            bool fullyBlocked = isEnemyAttackAgainstPlayer
                && resultsAgainstPlayer.All(result => result.UnblockedDamage <= 0);

            GuardCounterPower guardCounter = playerCreature.GetPower<GuardCounterPower>();
            if (guardCounter != null)
            {
                await guardCounter.ResolveAfterAttack(command);
            }

            StalwartShieldPower stalwart = playerCreature.GetPower<StalwartShieldPower>();
            if (stalwart != null && isEnemyAttackAgainstPlayer)
                stalwart.AfterEnemyAttackResolved(fullyBlocked);

            if (!fullyBlocked)
                continue;

            BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
            StormBarrierPower barrier = playerCreature.GetPower<StormBarrierPower>();
            if (barrier != null)
                await barrier.AfterFullyBlockedAttack(context, attacker);

            RetreatingDefensePower retreatingDefense = playerCreature.GetPower<RetreatingDefensePower>();
            if (retreatingDefense != null)
                retreatingDefense.AfterFullyBlockedAttack();

        }

        return command;
    }
}
