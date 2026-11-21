using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using NightMustStay.Core.Models.CardPools;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers), "CardPoolColorOrder", MethodType.Getter)]
internal static class ColorfulPhilosophersPatch
{
    private static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        CardPoolModel[] nightfarerPools =
        {
            ModelDb.CardPool<GuardianCardPool>(),
            ModelDb.CardPool<IroneyeCardPool>(),
            ModelDb.CardPool<RevenantCardPool>(),
            ModelDb.CardPool<DuchessCardPool>(),
            ModelDb.CardPool<WylderCardPool>()
        };
        __result = __result.Concat(nightfarerPools).DistinctBy(pool => pool.Id);
    }
}
