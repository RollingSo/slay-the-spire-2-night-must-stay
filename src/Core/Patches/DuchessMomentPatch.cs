using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch]
public static class DuchessMomentPatch
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    [HarmonyPostfix]
    public static void AttachAtCombatStart(ICombatState __1, ref Task __result) =>
        __result = AttachAfterHooks(__result, __1);

    private static async Task AttachAfterHooks(Task original, ICombatState combatState)
    {
        await original;
        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsAlive)
                await DuchessMomentPower.Ensure(new BlockingPlayerChoiceContext(), player.Creature);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
    [HarmonyPostfix]
    public static void RepairMissingController(CardPlay __1, ref Task __result)
    {
        if (__1.Card.Owner?.Creature is { IsAlive: true } owner && !owner.HasPower<DuchessMomentPower>())
            __result = RepairAfterHooks(__result, __1);
    }

    private static async Task RepairAfterHooks(Task original, CardPlay play)
    {
        await original;
        if (!play.Card.Owner.Creature.HasPower<DuchessMomentPower>())
            await DuchessMomentPower.Ensure(new BlockingPlayerChoiceContext(), play.Card.Owner.Creature);
    }
}
