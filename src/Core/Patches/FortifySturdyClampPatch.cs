using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.ShouldClearBlock))]
    public static class FortifySturdyClampPatch
    {
        [HarmonyPostfix]
        public static void PreferFortifyForCombinedRetention(
            Creature creature,
            ref bool __result,
            ref AbstractModel preventer)
        {
            if (__result || preventer is not SturdyClamp)
                return;

            FortifyPower fortify = creature?.GetPower<FortifyPower>();
            if (fortify != null)
                preventer = fortify;
        }
    }
}
