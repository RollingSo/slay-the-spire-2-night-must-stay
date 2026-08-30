using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Relics;
using NightMustStay.Core.Telemetry;

namespace NightMustStay
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
            Core.Compatibility.Sts2BranchCompat.RegisterSavedPropertyType(typeof(EvolutionWings));
            Core.Compatibility.Sts2BranchCompat.RegisterSavedPropertyType(typeof(FlyingFeatherHelm));

            var harmony = new HarmonyLib.Harmony("NightMustStay.author");
            harmony.PatchAll();

            // 战局数据上报（异步，不影响游戏；AppData 配置可覆盖 DLL 内置默认值）
            TelemetryService.Initialize();

            Log.Info("Slay the Spire 2 : Night Must Stay loaded");
        }
    }
}
