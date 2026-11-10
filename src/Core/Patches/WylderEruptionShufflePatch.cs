using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle))]
internal static class WylderEruptionShufflePatch
{
    [HarmonyPostfix]
    // Bind the choice context by position instead of by the engine's parameter name.
    // Public and beta builds have used both `context` and `choiceContext` here.
    private static void EnterPursuit(PlayerChoiceContext __0, Player player, ref Task __result) =>
        __result = Finish(__result, __0, player);

    private static async Task Finish(Task native, PlayerChoiceContext context, Player player)
    {
        await native;
        if (player?.Creature?.GetPower<WylderEruptionPower>() is not { } eruption)
            return;

        await eruption.AfterShuffle(context);
    }
}
