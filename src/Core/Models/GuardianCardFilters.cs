using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models
{
    internal static class GuardianCardFilters
    {
        // Canonical Chinese-name membership; translations must not change mechanics.
        // Keep aligned with design/card_name_localization.json.
        private static readonly HashSet<string> DefendCardIds = new(StringComparer.Ordinal)
        {
            "ABSOLUTE_DEFENSE",
            "DEFEND_GUARDIAN",
            "DEFEND_IRONEYE",
            "DEFEND_REVENANT",
            "DEFENSIVE_REINFORCEMENT",
            "DUCHESS_DEFEND",
            "EMERGENCY_DEFEND",
            "EVOLVED_DEFEND",
            "GUARD_COUNTER_CARD",
            "POWERFUL_DEFEND",
            "POWERFUL_GUARD_COUNTER",
            "PROTECTIVE_AIRSTREAM",
            "RETREATING_DEFENSE",
            "SHARED_GREAT_SHIELD",
            "SLOW_DEFEND",
            "ULTIMATE_DEFEND_COUNTER",
        };

        public static bool HasDefendInName(CardModel card)
        {
            return HasDefendInName(card.Id.Entry, card.Title,
                card.GetType().Assembly == typeof(GuardianCardFilters).Assembly);
        }

        internal static bool HasDefendInName(string cardId, string title, bool isModCard)
        {
            if (isModCard)
                return DefendCardIds.Contains(cardId);

            return title.Contains("防御", StringComparison.OrdinalIgnoreCase)
                || title.Contains("Defend", StringComparison.OrdinalIgnoreCase)
                || title.Contains("Defense", StringComparison.OrdinalIgnoreCase)
                || title.Contains("수비", StringComparison.OrdinalIgnoreCase);
        }
    }
}
