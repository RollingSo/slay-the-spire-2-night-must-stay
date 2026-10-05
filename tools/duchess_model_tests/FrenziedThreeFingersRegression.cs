using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

internal static class FrenziedThreeFingersRegression
{
    private static Creature _enemy = null!;
    private static readonly List<decimal> DamageAmounts = new();

    public static void Run()
    {
        var harmony = new Harmony("night-must-stay.tests.three-fingers-fatal");
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var owner = new Creature(player, 70, 70);
        var state = new CombatState();
        owner.CombatState = state;
        typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, owner);
        typeof(Player).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, NullRunState.Instance);
        var manager = RevenantSummonManager.For(player);
        var power = (FrenziedThreeFingersPower)ModelDb.Power<FrenziedThreeFingersPower>().ToMutable();
        typeof(PowerModel).GetProperty("Owner")!.SetValue(power, owner);
        power.SetAmount(1, false);
        _enemy = new Creature(player, 100, 100);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatState), nameof(CombatState.HittableEnemies)),
            prefix: new HarmonyMethod(typeof(FrenziedThreeFingersRegression), nameof(EnemyTargets)));
        MethodInfo damage = typeof(Sts2BranchCompat).GetMethods().Single(method => method.Name == "Damage"
            && method.GetParameters().Length == 6 && method.GetParameters()[1].ParameterType == typeof(Creature)
            && method.GetParameters()[2].ParameterType == typeof(decimal));
        harmony.Patch(damage, prefix: new HarmonyMethod(typeof(FrenziedThreeFingersRegression), nameof(CaptureDamage)));
        try
        {
            var family = new Creature(player, 0, 6) { PetOwner = player };
            ((HashSet<Creature>)typeof(RevenantSummonManager).GetField("_knownFamilyCreatures", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).Add(family);
            // CurrentFamilyCreature is absent, as it is after family death cleanup.
            Check(power, family, 6, 99, true, 6);
            Check(power, family, 0, 0, false, null);
            var livingFamily = new Creature(player, 3, 6) { PetOwner = player };
            ((HashSet<Creature>)typeof(RevenantSummonManager).GetField("_knownFamilyCreatures", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!).Add(livingFamily);
            Check(power, livingFamily, 3, 0, false, 3);
            var necro = new Creature(player, 0, 20) { PetOwner = player };
            manager.RegisterNecro(new RevenantNecro { Creature = necro, SourceMonster = null! });
            Check(power, necro, 20, 50, true, 20);
            Check(power, new Creature(player, 0, 20), 20, 50, true, null);
            power.SetAmount(2, false);
            Check(power, family, 6, 99, true, 12);
            if (typeof(FrenziedThreeFingersPower).GetMethod(nameof(PowerModel.AfterDamageReceived))!.DeclaringType == typeof(FrenziedThreeFingersPower))
                throw new Exception("Three Fingers must not also trigger on the nonlethal received hook.");
            Console.WriteLine("PASS: Three Fingers triggers once for lethal Family/Necro damage, counts actual HP loss without overkill, and preserves nonlethal/stacked behavior (damage sink stubbed).");
        }
        finally { harmony.UnpatchAll(harmony.Id); RevenantSummonManager.Clear(player); }
    }

    private static void Check(FrenziedThreeFingersPower power, Creature target, int hpLost, int overkill, bool fatal, decimal? expected)
    {
        DamageAmounts.Clear();
        var result = new DamageResult(target, ValueProp.Unpowered)
            { UnblockedDamage = hpLost, OverkillDamage = overkill, WasTargetKilled = fatal };
        power.AfterDamageGiven(null!, null!, result, ValueProp.Unpowered, target, null!).GetAwaiter().GetResult();
        if (expected == null ? DamageAmounts.Count != 0 : DamageAmounts.Count != 1 || DamageAmounts[0] != expected)
            throw new Exception("Three Fingers fatal HP-loss trigger is incorrect or duplicated.");
    }

    public static bool EnemyTargets(ref IReadOnlyList<Creature> __result)
    { __result = new[] { _enemy }; return false; }

    public static bool CaptureDamage(decimal amount, ref Task<IEnumerable<DamageResult>> __result)
    { DamageAmounts.Add(amount); __result = Task.FromResult<IEnumerable<DamageResult>>(Array.Empty<DamageResult>()); return false; }
}
