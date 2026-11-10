using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Patches;

// A development pool can be smaller than the native shop's five distinct slots.
// Restore exhausted candidates, never introduce cards from another character.
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant),
    new[] { typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType) })]
internal static class WylderSmallShopPoolPatch
{
    [HarmonyPrefix]
    private static void Repair(Player player, ref IEnumerable<CardModel> options, ref CardType type)
    {
        if (player.Character is not Wylder) return;
        var supplied = options.ToArray();
        var requestedType = type;
        if (supplied.Any(c => c.Type == requestedType && c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)) return;
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
        Restore(pool, ref options, ref type);
    }
    internal static void Restore(CardModel[] pool, ref IEnumerable<CardModel> options, ref CardType type)
    {
        var requestedType = type;
        if (pool.Length == 0) return;
        if (!pool.Any(c => c.Type == requestedType)) type = pool[0].Type;
        options = pool;
    }
}

[HarmonyPatch]
internal static class WylderSmallRewardPoolPatch
{
    private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(CardFactory))
        .Single(m => m.Name == "CreateForReward" && m.IsPrivate && m.GetParameters().Length == 3);
    [HarmonyPrefix]
    private static void Repair(Player player, ref IEnumerable<CardModel> blacklist, CardCreationOptions options)
    {
        if (player.Character is not Wylder) return;
        var candidates = options.GetPossibleCards(player).Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
        Restore(candidates, ref blacklist);
    }
    internal static void Restore(CardModel[] candidates, ref IEnumerable<CardModel> blacklist)
    {
        if (candidates.Length > 0 && !candidates.Except(blacklist).Any()) blacklist = System.Array.Empty<CardModel>();
    }
}
