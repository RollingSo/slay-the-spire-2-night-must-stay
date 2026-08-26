using System;
using System.Collections.Generic;
using System.Linq;
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
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantCharge,
                "[gold]蓄力[/gold]", "[gold]Charge[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantRecover,
                "[gold]回收[/gold]", "[gold]Recover[/gold]", "[gold]Recovered[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantCall,
                "[gold]呼唤[/gold]", "[gold]Call[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantResonance,
                "[gold]共鸣[/gold]", "[gold]Resonance[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantFamily,
                "[gold]家人[/gold]", "[gold]Family[/gold]", "[gold]family member[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantNecro,
                "[gold]死灵[/gold]", "[gold]Necro[/gold]", "[gold]Necros[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.HelenActions,
                "[gold]海伦[/gold]", "[gold]Helen[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.FrederickActions,
                "[gold]弗雷德利克[/gold]", "[gold]Frederick[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.SebastianActions,
                "[gold]塞巴斯蒂安[/gold]", "[gold]Sebastian[/gold]");

            AddIfMentioned(tips, text, HoverTipFactory.FromPower<WeakPower>(),
                "[gold]虚弱[/gold]", "[gold]Weak[/gold]");
            AddIfMentioned(tips, text, HoverTipFactory.FromPower<VulnerablePower>(),
                "[gold]易伤[/gold]", "[gold]Vulnerable[/gold]");
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
            AddIfMentioned(tips, text, HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
                "[gold]虚无[/gold]", "[gold]Ethereal[/gold]");

            if (text.Contains("energyIcons", StringComparison.Ordinal))
                tips.MegaTryAddingTip(HoverTipFactory.ForEnergy(__instance));

            __result = tips;
        }

        private static string GetAllDescriptionText(CardModel card)
        {
            string id = card.Id.Entry;
            string text = card.Description.Exists() ? card.Description.GetRawText() : string.Empty;
            string[] supplementalKeys =
            {
                id + ".upgradeDescription",
                id + ".unchargedDescription",
                id + ".chargedDescription",
            };
            foreach (string key in supplementalKeys)
            {
                if (LocString.Exists("cards", key))
                    text += "\n" + new LocString("cards", key).GetRawText();
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

        private static void AddAllIfMentioned(
            ICollection<IHoverTip> tips,
            string text,
            IEnumerable<IHoverTip> additions,
            params string[] tokens)
        {
            if (!tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase)))
                return;

            foreach (IHoverTip tip in additions)
                tips.MegaTryAddingTip(tip);
        }
    }

    [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.HoverTips), MethodType.Getter)]
    public static class RevenantPowerHoverTipPatch
    {
        [HarmonyPostfix]
        public static void AddFamilyGlossaryTips(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (__instance.GetType().Namespace != "sts2mod.Core.Models.Power")
                return;

            string text = __instance.Description.Exists()
                ? __instance.Description.GetRawText()
                : string.Empty;
            var tips = new List<IHoverTip>(__result);

            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantFamily,
                "[gold]家人[/gold]", "[gold]Family[/gold]", "[gold]family member[/gold]");
            AddIfMentioned(tips, text, GuardianCardHoverTips.RevenantNecro,
                "[gold]死灵[/gold]", "[gold]Necro[/gold]", "[gold]Necros[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.HelenActions,
                "[gold]海伦[/gold]", "[gold]Helen[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.FrederickActions,
                "[gold]弗雷德利克[/gold]", "[gold]Frederick[/gold]");
            AddAllIfMentioned(tips, text, GuardianCardHoverTips.SebastianActions,
                "[gold]塞巴斯蒂安[/gold]", "[gold]Sebastian[/gold]");

            __result = tips;
        }

        private static void AddIfMentioned(
            ICollection<IHoverTip> tips,
            string text,
            IHoverTip tip,
            params string[] tokens)
        {
            if (tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase)))
                tips.MegaTryAddingTip(tip);
        }

        private static void AddAllIfMentioned(
            ICollection<IHoverTip> tips,
            string text,
            IEnumerable<IHoverTip> additions,
            params string[] tokens)
        {
            if (!tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase)))
                return;

            foreach (IHoverTip tip in additions)
                tips.MegaTryAddingTip(tip);
        }
    }
}
