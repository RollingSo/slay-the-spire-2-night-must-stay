using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch]
    public static class GuardianArchaicToothPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(typeof(ArchaicTooth), "TranscendenceUpgrades");

        [HarmonyPostfix]
        private static void AddGuardianTranscendence(
            ref Dictionary<ModelId, CardModel> __result)
        {
            __result[ModelDb.Card<StompStance>().Id] =
                ModelDb.Card<UnbreakableStance>();
            __result[ModelDb.Card<IroneyeMark>().Id] =
                ModelDb.Card<DeathMark>();
        }
    }
}
