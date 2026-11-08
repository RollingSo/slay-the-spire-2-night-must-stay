using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using NightMustStay.Core.Models.Power;

internal static class FortifyRetentionRegression
{
    public static void Run()
    {
        var harmony = new Harmony("night-must-stay.tests.fortify-native-clear");
        try
        {
            // Install the sink before compiling patched async callbacks, so
            // native async wrapper inlining cannot bypass the fixture.
            harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.LoseBlock)),
                prefix: new HarmonyMethod(typeof(FortifyRetentionRegression), nameof(LoseBlock)));
            Type patch = typeof(FortifyPower).Assembly.GetType("NightMustStay.Core.Patches.FortifyAutomaticBlockRetentionPatch", true)!;
            harmony.CreateClassProcessor(patch).Patch();
            foreach (string name in new[] { "FortifySturdyClampRetentionPatch", "FortifySturdyClampNotificationPatch", "FortifySturdyClampBarricadePatch" })
                harmony.CreateClassProcessor(typeof(FortifyPower).Assembly.GetType("NightMustStay.Core.Patches." + name, true)!).Patch();
            if (!ModelDb.Contains(typeof(SturdyClamp))) ModelDb.Inject(typeof(SturdyClamp));
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
            foreach (int block in new[] { 0, 3, 10, 20, 40 })
            foreach (int amount in new[] { 0, 5, 10, 15, 50 })
            foreach (bool wall in new[] { false, true })
            foreach (bool wallFirst in new[] { false, true })
                Check(block, amount, wall, wallFirst,
                    wall && (amount > 0 || wallFirst) ? block : Math.Min(block, Math.Max(10, amount)), clamp: true);
            if (typeof(FortifyPower).GetMethod(nameof(PowerModel.ShouldClearBlock))!.DeclaringType == typeof(FortifyPower)
                || typeof(FortifyPower).GetMethod(nameof(PowerModel.AfterPreventingBlockClear))!.DeclaringType == typeof(FortifyPower))
                throw new Exception("Fortify must neither intercept prevention nor actively remove Block.");
            Console.WriteLine("PASS: native automatic clear and Sturdy Clamp retention use the higher allowance; Barricade preserves all Block in either order; explicit loss remains unchanged.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Check(int block, int amount, bool barricade, bool barricadeFirst, int expected, bool clamp = false)
    {
        var player = TrackingArrowRegression.MakePlayer();
        var creature = new Creature(player, 70, 70) { CombatState = new CombatState() };
        typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, creature);
        var powers = (List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(creature)!;
        var fortify = (FortifyPower)ModelDb.Power<FortifyPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(fortify, creature);
        fortify.SetAmount(amount, false);
        int flashes = 0;
        fortify.Flashed += _ => flashes++;
        var sharing = ModelDb.Power<GuardianMultiplayerPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(sharing, creature);
        sharing.SetAmount(2, false);
        powers.Add(sharing);
        var wall = ModelDb.Power<BarricadePower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(wall, creature);
        if (barricade && barricadeFirst) powers.Add(wall);
        if (amount > 0) powers.Add(fortify);
        if (barricade && !barricadeFirst) powers.Add(wall);
        var relics = new List<RelicModel>();
        if (clamp)
        {
            var relic = ModelDb.Relic<SturdyClamp>().ToMutable();
            relic.Owner = player;
            relics.Add(relic);
        }
        typeof(Player).GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, relics);
        _relicsFirst = !barricadeFirst;
        typeof(Creature).GetProperty(nameof(Creature.Block))!.SetValue(creature, block);
        _lossCalls = 0; _callback = "none";
        ((Task)AccessTools.Method(typeof(Creature), "ClearBlock").Invoke(creature, null)!).GetAwaiter().GetResult();
        if (creature.Block != expected) throw new Exception($"Block {block}, Fortify {amount}, Barricade {barricade}, Clamp {clamp}: expected {expected}, got {creature.Block}; loss calls {_lossCalls}, preventer {_callback}.");
        int attributed = barricade ? 0 : Math.Min(block, Math.Max(0, amount));
        if (flashes != (attributed > 0 ? 1 : 0)) throw new Exception("Fortify retention must notify exactly once, only when it applies.");
        decimal pending = (decimal)AccessTools.Field(typeof(GuardianMultiplayerPower), "_pendingTeammateBlock").GetValue(sharing)!;
        if (pending != 2m * attributed) throw new Exception("Clamp must not omit or double-count Fortify's teammate reward.");
        // An explicit loss must not be adjusted by automatic-clear retention.
        creature.LoseBlockInternal(2);
        if (creature.Block != Math.Max(0, expected - 2)) throw new Exception("Fortify must not affect ordinary Block loss.");
    }

    public static bool CheckPreventers(Creature creature, ref AbstractModel preventer, ref bool __result)
    {
        var powers = creature.Powers.Cast<AbstractModel>();
        var relics = creature.Player!.Relics.Cast<AbstractModel>();
        preventer = (_relicsFirst ? relics.Concat(powers) : powers.Concat(relics))
            .FirstOrDefault(model => !model.ShouldClearBlock(creature))!;
        __result = preventer == null;
        return false;
    }

    private static bool _relicsFirst;
    private static int _lossCalls;
    private static string _callback = "none";

    public static bool Prevented(AbstractModel preventer, Creature creature, ref Task __result)
    {
        _callback = preventer.GetType().Name;
        __result = preventer.AfterPreventingBlockClear(preventer, creature);
        return false;
    }

    public static bool LoseBlock(object[] __args, ref Task __result)
    {
        _lossCalls++;
        __args.OfType<Creature>().First().LoseBlockInternal(__args.OfType<decimal>().Single());
        __result = Task.CompletedTask;
        return false;
    }
}
