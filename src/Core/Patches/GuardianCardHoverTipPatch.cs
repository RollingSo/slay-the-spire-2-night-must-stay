using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    public static class GuardianCardHoverTipPatch
    {
        [HarmonyPostfix]
        public static void AddGlossaryTips(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance.GetType().Namespace != "sts2mod.Core.Models.Cards")
                return;

            string text = GetAllDescriptionText(__instance);
            var tips = new List<IHoverTip>(__result);

            AddIfMentioned(tips, text, GuardianCardHoverTips.Synthesis,
                "[gold]合成[/gold]", "[gold]Synthesize[/gold]", "[gold]Synthesis[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.ConcealedEdge,
                "[gold]\u85cf\u950b[/gold]", "[gold]Concealed Edge[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<FortifyPower>(),
                "[gold]固守[/gold]", "[gold]Fortify[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<GuardCounterPower>(),
                "[gold]防御反击[/gold]", "[gold]Guard Counter[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromCard<ShieldPoke>(),
                "[gold]盾戳[/gold]", "[gold]Shield Poke[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<PhantomImbalancePower>(),
                "[gold]失衡[/gold]", "[gold]Imbalance[/gold]");

            AddIfMentioned(tips, text, HoverTipFactory.FromPower<WeakPower>(),
                "[gold]虚弱[/gold]", "[gold]Weak[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.Static(StaticHoverTip.Block),
                "[gold]格挡[/gold]", "[gold]Block[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<StrengthPower>(),
                "[gold]力量[/gold]", "[gold]Strength[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<DexterityPower>(),
                "[gold]敏捷[/gold]", "[gold]Dexterity[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.Static(StaticHoverTip.Stun),
                "[gold]击晕[/gold]", "[gold]Stun[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromKeyword(CardKeyword.Retain),
                "[gold]保留[/gold]", "[gold]Retain[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
                "[gold]消耗[/gold]", "[gold]Exhaust[/gold]", "[gold]Exhausted[/gold]");

            if (text.Contains("energyIcons", StringComparison.Ordinal))
                tips.MegaTryAddingTip(HoverTipFactory.ForEnergy(__instance));

            __result = tips;
        }

        private static string GetAllDescriptionText(CardModel card)
        {
            string id = card.Id.Entry;
            string text = card.Description.Exists() ? card.Description.GetRawText() : string.Empty;
            if (LocString.Exists("cards", id + ".upgradeDescription"))
            {
                text += "\n" + new LocString("cards", id + ".upgradeDescription").GetRawText();
            }

            return text;
        }

        private static void AddIfMentioned(
            ICollection<IHoverTip> tips,
            string text,
            IHoverTip tip,
            params string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (!text.Contains(token, StringComparison.OrdinalIgnoreCase))
                    continue;

                tips.MegaTryAddingTip(tip);
                return;
            }
        }
    }
}
