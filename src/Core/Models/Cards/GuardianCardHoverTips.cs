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

        public static IHoverTip RevenantCharge => new HoverTip(
            new LocString("cards", "REVENANT_CHARGE.tooltipTitle"),
            new LocString("cards", "REVENANT_CHARGE.tooltipDescription"));

        public static IHoverTip RevenantRecover => new HoverTip(
            new LocString("cards", "REVENANT_RECOVER.tooltipTitle"),
            new LocString("cards", "REVENANT_RECOVER.tooltipDescription"));

        public static IHoverTip RevenantCall => new HoverTip(
            new LocString("cards", "REVENANT_CALL.tooltipTitle"),
            new LocString("cards", "REVENANT_CALL.tooltipDescription"));

        public static IHoverTip RevenantResonance => new HoverTip(
            new LocString("cards", "REVENANT_RESONANCE.tooltipTitle"),
            new LocString("cards", "REVENANT_RESONANCE.tooltipDescription"));
    }
}
