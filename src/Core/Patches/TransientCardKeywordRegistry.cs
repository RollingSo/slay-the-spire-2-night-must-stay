using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Patches
{
    internal static class TransientCardKeywordRegistry
    {
        private static readonly Dictionary<Player, HashSet<CardModel>> RetainedCardsByPlayer = new();

        public static void TrackRetain(CardModel card)
        {
            Player player = card.Owner;
            if (!RetainedCardsByPlayer.TryGetValue(player, out HashSet<CardModel> cards))
            {
                cards = new HashSet<CardModel>();
                RetainedCardsByPlayer.Add(player, cards);
            }

            cards.Add(card);
        }

        public static void ClearFor(Player player)
        {
            if (!RetainedCardsByPlayer.Remove(player, out HashSet<CardModel> cards))
                return;

            foreach (CardModel card in cards)
                card.RemoveKeyword(CardKeyword.Retain);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
    internal static class GuardianTransientCardKeywordCleanupPatch
    {
        [HarmonyPrefix]
        private static void RemoveCombatOnlyKeywords(Player __instance)
        {
            TransientCardKeywordRegistry.ClearFor(__instance);
        }
    }
}
