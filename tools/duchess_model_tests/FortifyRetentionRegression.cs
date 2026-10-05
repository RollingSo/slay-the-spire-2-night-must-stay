using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;

internal static class FortifyRetentionRegression
{
    public static void Run()
    {
        var harmony = new Harmony("night-must-stay.tests.fortify-native-clear");
        try
        {
            Type patch = typeof(FortifyPower).Assembly.GetType("NightMustStay.Core.Patches.FortifyAutomaticBlockRetentionPatch", true)!;
            harmony.CreateClassProcessor(patch).Patch();
            harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ShouldClearBlock)),
                prefix: new HarmonyMethod(typeof(FortifyRetentionRegression), nameof(CheckPreventers)));
            // No combat state/assets in this fixture; the native prevention
            // branch is preserved while unrelated notification sinks are stubbed.
            harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.AfterPreventingBlockClear)),
                prefix: new HarmonyMethod(typeof(FortifyRetentionRegression), nameof(Prevented)));
            Check(30, 5, false, false, 5);
            Check(3, 5, false, false, 3);
            Check(0, 5, false, false, 0);
            Check(30, 0, false, false, 0);
            Check(30, 5, true, false, 30);
            Check(30, 5, true, true, 30);
            if (typeof(FortifyPower).GetMethod(nameof(PowerModel.ShouldClearBlock))!.DeclaringType == typeof(FortifyPower)
                || typeof(FortifyPower).GetMethod(nameof(PowerModel.AfterPreventingBlockClear))!.DeclaringType == typeof(FortifyPower))
                throw new Exception("Fortify must neither intercept prevention nor actively remove Block.");
            Console.WriteLine("PASS: real native automatic Block clear retains only Fortify allowance; Barricade preserves all Block in either power order.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Check(int block, int amount, bool barricade, bool barricadeFirst, int expected)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = new Creature(player, 70, 70);
        typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, creature);
        var powers = (List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(creature)!;
        var fortify = (FortifyPower)ModelDb.Power<FortifyPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(fortify, creature);
        fortify.SetAmount(amount, false);
        var wall = ModelDb.Power<BarricadePower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(wall, creature);
        if (barricade && barricadeFirst) powers.Add(wall);
        if (amount > 0) powers.Add(fortify);
        if (barricade && !barricadeFirst) powers.Add(wall);
        typeof(Creature).GetProperty(nameof(Creature.Block))!.SetValue(creature, block);
        ((Task)AccessTools.Method(typeof(Creature), "ClearBlock").Invoke(creature, null)!).GetAwaiter().GetResult();
        if (creature.Block != expected) throw new Exception($"Block {block}, Fortify {amount}, Barricade {barricade}: expected {expected}, got {creature.Block}.");
        // An explicit loss must not be adjusted by automatic-clear retention.
        creature.LoseBlockInternal(2);
        if (creature.Block != Math.Max(0, expected - 2)) throw new Exception("Fortify must not affect ordinary Block loss.");
    }

    public static bool CheckPreventers(Creature creature, ref AbstractModel preventer, ref bool __result)
    {
        preventer = creature.Powers.FirstOrDefault(power => !power.ShouldClearBlock(creature))!;
        __result = preventer == null;
        return false;
    }

    public static bool Prevented(ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}
