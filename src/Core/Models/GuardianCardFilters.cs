using System;
using MegaCrit.Sts2.Core.Models;

namespace sts2mod.Core.Models
{
    internal static class GuardianCardFilters
    {
        public static bool HasDefendInName(CardModel card)
        {
            string title = card.Title;
            return title.Contains("防御", StringComparison.OrdinalIgnoreCase)
                || title.Contains("Defend", StringComparison.OrdinalIgnoreCase)
                || title.Contains("Defense", StringComparison.OrdinalIgnoreCase);
        }
    }
}
