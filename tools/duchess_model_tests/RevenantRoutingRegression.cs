using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

internal static class RevenantRoutingRegression
{
    private static readonly List<(Creature Target, decimal Amount)> Hits = new();

    public static void Run()
    {
        var harmony = new Harmony("night-must-stay.tests.necro-first-routing");
        MethodInfo damage = typeof(Sts2BranchCompat).GetMethods().Single(method =>
            method.Name == "Damage" && method.GetParameters().Length == 6
            && method.GetParameters()[1].ParameterType == typeof(Creature)
            && method.GetParameters()[2].ParameterType == typeof(decimal));
        harmony.Patch(damage, prefix: new HarmonyMethod(typeof(RevenantRoutingRegression), nameof(CaptureDamage)));
        try
        {
            Check(10, 20, 11, false, new[] { ("necro", 10m) });
            Check(25, 20, 11, false, new[] { ("necro", 20m), ("family", 5m) });
            Check(40, 20, 11, false, new[] { ("necro", 20m), ("family", 11m), ("player", 9m) });
            Check(25, 0, 11, false, new[] { ("family", 11m), ("player", 14m) });
            Check(25, 20, 0, false, new[] { ("necro", 20m), ("player", 5m) });
            Check(25, 0, 0, false, Array.Empty<(string, decimal)>());
            Check(40, 20, 11, true, new[] { ("necro", 40m) });
            Console.WriteLine("PASS: actual Necro -> Family -> player damage routing, overflow, missing/dead summons and Buffer protection (damage sink stubbed).");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Check(decimal incoming, int necroHp, int familyHp, bool buffer,
        (string Name, decimal Amount)[] expected)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var owner = new Creature(player, 70, 70);
        typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, owner);
        var necro = new Creature(player, necroHp, 20) { PetOwner = player };
        var family = new Creature(player, familyHp, 11) { PetOwner = player };
        var manager = RevenantSummonManager.For(player);
        manager.RegisterNecro(new RevenantNecro { Creature = necro, SourceMonster = null! });
        typeof(RevenantSummonManager).GetField("_familyCreature", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(manager, family);
        var controller = (RevenantSummonControllerPower)ModelDb.Power<RevenantSummonControllerPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(controller, owner);
        if (buffer)
        {
            var power = ModelDb.Power<BufferPower>().ToMutable();
            typeof(PowerModel).GetProperty("Owner")!.SetValue(power, necro);
            ((List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(necro)!).Add(power);
        }
        try
        {
            Hits.Clear();
            decimal result = controller.ModifyHpLostBeforeOstyLate(owner, incoming, ValueProp.Move, null!, null!);
            bool protectedBySummon = necroHp > 0 || familyHp > 0;
            if (result != (protectedBySummon ? 0 : incoming)) throw new Exception("Initial routing interception is incorrect.");
            controller.AfterModifyingHpLostBeforeOsty().GetAwaiter().GetResult();
            var actual = Hits.Select(hit => (hit.Target == necro ? "necro" : hit.Target == family ? "family" : "player", hit.Amount)).ToArray();
            if (!actual.SequenceEqual(expected)) throw new Exception($"Unexpected damage route: {string.Join(", ", actual)}.");
            Hits.Clear();
            if (controller.ModifyHpLostBeforeOstyLate(owner, incoming, ValueProp.Unpowered, null!, null!) != incoming
                || controller.ModifyHpLostBeforeOstyLate(necro, incoming, ValueProp.Move, null!, null!) != incoming)
                throw new Exception("Non-attack and directly targeted summon damage must not be rerouted.");
            controller.AfterModifyingHpLostBeforeOsty().GetAwaiter().GetResult();
            if (Hits.Count != 0) throw new Exception("Routing must consume the pending hit exactly once.");
        }
        finally { RevenantSummonManager.Clear(player); }
    }

    public static bool CaptureDamage(Creature target, decimal amount,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        Hits.Add((target, amount));
        __result = Task.FromResult<IEnumerable<DamageResult>>(Array.Empty<DamageResult>());
        return false;
    }
}
