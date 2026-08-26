using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Relics;
using sts2mod.Core.Telemetry;

namespace sts2mod
{
    [ModInitializer(nameof(Initialize))]
    public static class ModInitializer
    {
        public static void Initialize()
        {
            // Mod model types are not part of AbstractModelSubtypes when the
            // game's saved-property cache is initialized. Register every
            // Mod model that owns [SavedProperty] state so permanent card
            // growth and relic counters survive save/load and act transitions.
            SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(EvolutionWings));
            SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FlyingFeatherHelm));

            var harmony = new HarmonyLib.Harmony("sts2mod.author");
            harmony.PatchAll();

            // 战局数据上报（异步，不影响游戏；未配置 config.json 时自动禁用）
            TelemetryService.Initialize();

            Log.Info("Slay the Spire 2 : Night Must Stay loaded");
        }
    }
}
