using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Relics;

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

            var harmony = new Harmony("sts2mod.author");
            harmony.PatchAll();

            Log.Info("Slay the Spire 2 : Night Must Stay loaded");
        }
    }
}
