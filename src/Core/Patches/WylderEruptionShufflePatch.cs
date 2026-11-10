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
    private static void EnterPursuit(PlayerChoiceContext context, Player player, ref Task __result) =>
        __result = Finish(__result, context, player);

    private static async Task Finish(Task native, PlayerChoiceContext context, Player player)
    {
        await native;
        if (player?.Creature?.GetPower<WylderEruptionPower>() is not { } eruption)
            return;

        await eruption.AfterShuffle(context);
    }
}
