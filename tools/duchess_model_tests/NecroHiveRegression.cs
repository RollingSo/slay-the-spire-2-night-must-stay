using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;
using NightMustStay.Core.Patches;

internal static class NecroHiveRegression
{
    private static Creature? _recipient;

    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.necro-hive");
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var owner = new Creature(player, 70, 70);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, owner);
        var manager = RevenantSummonManager.For(player);
        var necro = new Creature(player, 20, 20) { PetOwner = player };
        manager.RegisterNecro(new RevenantNecro { Creature = necro, SourceMonster = null! });
        var hook = AccessTools.Method(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived));
        harmony.Patch(hook, prefix: new HarmonyMethod(typeof(RevenantPersonalHivePatch), nameof(RevenantPersonalHivePatch.ResolveNecroCardRecipient)));
        // Capture the recipient after the production prefix, without invoking
        // the native card-generation/UI machinery in this standalone fixture.
        harmony.Patch(hook, prefix: new HarmonyMethod(typeof(NecroHiveRegression), nameof(CaptureRecipient)) { priority = Priority.Last });
        try
        {
            var hive = (PersonalHivePower)RuntimeHelpers.GetUninitializedObject(typeof(PersonalHivePower));
            foreach (Creature? dealer in new Creature?[] { necro, owner, new Creature(player, 20, 20), null })
            {
                hive.AfterDamageReceived(null!, owner, null!, ValueProp.Move, dealer!, null!).GetAwaiter().GetResult();
                if (!ReferenceEquals(_recipient, dealer == necro ? owner : dealer))
                    throw new Exception("Hive must resolve only registered Necros to their player's creature.");
            }
            var lightning = ModelDb.Card<LightningStrike>().ToMutable();
            if (lightning.EnergyCost.Canonical != 0
                || lightning.EnergyCost.Canonical != ModelDb.Card<DuchessFallingMagic>().EnergyCost.Canonical)
                throw new Exception("Lightning Strike cost must match Falling Magic.");
            lightning.UpgradeInternal();
            if (lightning.EnergyCost.Canonical != 0 || lightning.DynamicVars.Damage.BaseValue != 9)
                throw new Exception("Lightning Strike upgrade must preserve 0 cost and 9 damage.");
            Console.WriteLine("PASS: native Hive hook receives Necro owner; player/other/null dealers stay unchanged (card generation stubbed). Lightning Strike matches Falling Magic cost before/after upgrade.");
        }
        finally { harmony.UnpatchAll(harmony.Id); RevenantSummonManager.Clear(player); }
    }

    public static bool CaptureRecipient(Creature dealer, ref Task __result)
    { _recipient = dealer; __result = Task.CompletedTask; return false; }
}
