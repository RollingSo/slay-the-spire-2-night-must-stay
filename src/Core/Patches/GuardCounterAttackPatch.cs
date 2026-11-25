using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Cards;
using System.Linq;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute), typeof(PlayerChoiceContext))]
public static class GuardCounterAttackPatch
{
    [HarmonyPrefix]
    private static void CaptureCombat(AttackCommand __instance, out ICombatState __state) =>
        __state = __instance.Attacker?.CombatState;

    [HarmonyPostfix]
    public static void AfterAttack(ref Task<AttackCommand> __result, ICombatState __state)
    {
        __result = ResolveGuardCounters(__result, __state);
    }

    private static async Task<AttackCommand> ResolveGuardCounters(Task<AttackCommand> attackTask, ICombatState startingCombat)
    {
        AttackCommand command = await attackTask;
        Creature attacker = command.Attacker;
        // v0.114 can detach an enemy that died during its own attack before this postfix runs.
        var combat = attacker?.CombatState ?? startingCombat;
        if (attacker == null || combat == null)
        {
            return command;
        }

        foreach (Creature playerCreature in combat.PlayerCreatures)
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
            if (playerCreature.Player != null)
                foreach (var charge in playerCreature.Player.PlayerCombatState.AllCards.OfType<GuardianCharge>().ToArray())
                    await charge.AfterFullyBlockedAttack();
            StormBarrierPower barrier = playerCreature.GetPower<StormBarrierPower>();
            if (barrier != null)
                await barrier.AfterFullyBlockedAttack(context, attacker);

            DuchessFullBlockRadiantBladePower carianRetaliation = playerCreature.GetPower<DuchessFullBlockRadiantBladePower>();
            if (carianRetaliation != null)
                await carianRetaliation.AfterFullyBlockedAttack(context);

        }

        return command;
    }
}
