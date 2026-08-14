using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
using sts2mod.Core.Models.Relics;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch]
    public static class GuardianTouchOfOrobasPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(typeof(TouchOfOrobas), "RefinementUpgrades");

        [HarmonyPostfix]
        private static void AddNightreignRefinements(
            ref Dictionary<ModelId, RelicModel> __result)
        {
            __result[ModelDb.Relic<SingleWingGreatshield>().Id] =
                ModelDb.Relic<TwinWingGreatshield>();
            __result[ModelDb.Relic<CursemarkSignet>().Id] =
                ModelDb.Relic<RunemarkSignet>();
        }
    }

    [HarmonyPatch(
        typeof(TouchOfOrobas),
        nameof(TouchOfOrobas.UpgradedRelic),
        MethodType.Setter)]
    public static class TouchOfOrobasLegacyRewardPatch
    {
        [HarmonyPrefix]
        public static void RecalculateLegacyFallback(
            TouchOfOrobas __instance,
            ref ModelId value)
        {
            if (value != ModelDb.Relic<Circlet>().Id ||
                __instance.StarterRelic == null)
                return;

            RelicModel starterRelic =
                SaveUtil.RelicOrDeprecated(__instance.StarterRelic);
            if (starterRelic is CursemarkSignet or SingleWingGreatshield)
            {
                value = __instance
                    .GetUpgradedStarterRelic(starterRelic)
                    .Id;
            }
        }
    }
}
