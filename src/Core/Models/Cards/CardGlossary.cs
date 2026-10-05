using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

/// <summary>Card rules explain mechanics; an unapplied power is not a rules preview.</summary>
internal static class CardGlossary
{
    internal static readonly Type[] Mechanics =
    {
        typeof(FortifyPower), typeof(GuardCounterPower), typeof(PhantomImbalancePower),
        typeof(DistancePower), typeof(LongShotPower), typeof(NightMustStayMarkPower),
        typeof(HiddenPoisonPower), typeof(PoisonBurstPower), typeof(FreezePower),
        typeof(DuchessConcealmentPower), typeof(DuchessMomentDescriptionPower),
        typeof(DuchessReactionDescriptionPower),
    };

    internal static bool IsMechanic(Type type) => Mechanics.Contains(type);
    internal static bool IsFamilyAction(Type type) => type == typeof(HelenStepStrikePower)
        || type == typeof(HelenRetreatPower) || type == typeof(FrederickHeavyHammerPower)
        || type == typeof(FrederickHeadbuttPower) || type == typeof(SebastianRoarPower)
        || type == typeof(SebastianSlamPower);

    internal static bool Mentions(string text, string title, bool moment = false) =>
        text.Contains("[gold]" + title + "[/gold]", StringComparison.OrdinalIgnoreCase)
        || (moment && text.Contains("[gold]" + title, StringComparison.OrdinalIgnoreCase));

    internal static IEnumerable<IHoverTip> Normalize(CardModel card, string text, IEnumerable<IHoverTip> original)
    {
        var result = new List<IHoverTip>();
        foreach (IHoverTip tip in original)
        {
            if (tip.CanonicalModel is not PowerModel power
                || power.GetType().Namespace != "NightMustStay.Core.Models.Power" || IsFamilyAction(power.GetType()))
            {
                result.MegaTryAddingTip(tip);
                continue;
            }
            // Do not repeat a card's own applied effect, or unrelated future-turn powers.
            // Preserve explicitly named statuses and independent mechanic definitions.
            if (IsMechanic(power.GetType()) || (Mentions(text, power.Title.GetFormattedText())
                && power.Id.Entry != card.Id.Entry + "_POWER"))
                result.MegaTryAddingTip(FromPower(power));
        }
        foreach (Type type in Mechanics)
        {
            var power = ModelDb.GetById<PowerModel>(ModelDb.GetId(type));
            if (Mentions(text, power.Title.GetFormattedText(), type == typeof(DuchessMomentDescriptionPower)))
                result.MegaTryAddingTip(FromPower(power));
        }
        foreach (string key in new[] { "GUARDIAN_SYNTHESIS", "GUARDIAN_CONCEALED_EDGE", "REVENANT_CHARGE",
                     "REVENANT_RECOVER", "REVENANT_CALL", "REVENANT_RESONANCE", "REVENANT_FAMILY", "REVENANT_NECRO" })
        {
            string suffix = key.StartsWith("REVENANT_", StringComparison.Ordinal) ? ".tooltip" : ".";
            var title = new LocString("cards", key + (suffix == ".tooltip" ? ".tooltipTitle" : ".title"));
            var description = new LocString("cards", key + (suffix == ".tooltip" ? ".tooltipDescription" : ".description"));
            if (title.Exists() && description.Exists() && Mentions(text, title.GetFormattedText()))
                result.MegaTryAddingTip(new HoverTip(title, description));
        }
        return result;
    }

    private static IHoverTip FromPower(PowerModel power)
    {
        LocString description = power switch
        {
            FortifyPower => new("cards", "NMS_GLOSSARY_FORTIFY.description"),
            FreezePower => new("cards", "NMS_GLOSSARY_FREEZE.description"),
            _ => power.Description,
        };
        power.DynamicVars.AddTo(description);
        description.Add("Amount", 1);
        description.Add("energyPrefix", EnergyIconHelper.GetPrefix(power));
        // Stable localization identity deduplicates the glossary; no power icon or canonical power.
        return new HoverTip(power.Title, description);
    }
}
