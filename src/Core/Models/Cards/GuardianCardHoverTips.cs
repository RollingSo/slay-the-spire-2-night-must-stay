using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace sts2mod.Core.Models.Cards
{
    internal static class GuardianCardHoverTips
    {
        public static IHoverTip Synthesis => new HoverTip(
            new LocString("cards", "GUARDIAN_SYNTHESIS.title"),
            new LocString("cards", "GUARDIAN_SYNTHESIS.description"));

        public static IHoverTip ConcealedEdge => new HoverTip(
            new LocString("cards", "GUARDIAN_CONCEALED_EDGE.title"),
            new LocString("cards", "GUARDIAN_CONCEALED_EDGE.description"));
    }
}
