using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;
using NightMustStay.Core.Models.Power;

internal static class SaviorWingsRegression
{
    private static bool _shortCircuit;
    public static void Run()
    {
        var h = new HarmonyLib.Harmony("night-must-stay.tests.savior-wings");
        var method = typeof(PowerModel).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == "ModifyDamageMultiplicative");
        h.Patch(AccessTools.Method(method.DeclaringType, method.Name),
            prefix: new HarmonyMethod(typeof(SaviorWingsRegression), nameof(OtherMod)) { priority = Priority.First });
        h.CreateClassProcessor(typeof(SaviorSpreadWingsPower).Assembly.GetType("NightMustStay.Core.Patches.SaviorSpreadWingsDamageBranchPatch")!).Patch();
        try
        {
            var player = TrackingArrowRegression.MakePlayer();
            Creature NewCreature(CombatSide side)
            {
                var c = new Creature(player, 100, 100);
                AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(c, side);
                return c;
            }
            var enemy = NewCreature(CombatSide.Enemy);
            var otherEnemy = NewCreature(CombatSide.Enemy);
            var power = ModelDb.Power<SaviorSpreadWingsPower>().ToMutable();
            AccessTools.PropertySetter(typeof(PowerModel), "Owner").Invoke(power, new object[] { enemy });
            var run = DispatchProxy.Create<IRunState, SaviorRunStateFixture>();
            ((SaviorRunStateFixture)(object)run).Listeners = new AbstractModel[] { power };
            foreach (bool shortCircuit in new[] { false, true })
            foreach (int count in new[] { 2, 3, 4 })
            foreach (int reduction in new[] { 50, 75, 125 })
            {
                _shortCircuit = shortCircuit; power.SetAmount(reduction, false);
                decimal expected = Math.Max(0m, (100m - reduction) / 100m);
                for (int i = 0; i < count; i++)
                {
                    var target = NewCreature(CombatSide.Player);
                    decimal Multiplier(Creature? dealer, ValueProp props)
                    {
                        var args = method.GetParameters().Select(p => p.Name switch {
                            "target" => (object)target, "amount" => 31m, "dealer" => dealer,
                            "props" => props, _ => null }).ToArray();
                        return (decimal)method.Invoke(power, args)!;
                    }
                    if (Multiplier(enemy, ValueProp.Move) != expected)
                        throw new Exception($"Savior Wings did not protect player {i + 1}/{count}, reduction={reduction}, another prefix skipped original={shortCircuit}.");
                    if (Multiplier(enemy, ValueProp.Unpowered) != 1m || Multiplier(otherEnemy, ValueProp.Move) != 1m
                        || Multiplier(target, ValueProp.Move) != 1m || Multiplier(null, ValueProp.Move) != 1m)
                        throw new Exception("Savior Wings reduced an unrelated damage source.");
                    decimal actual = Sts2BranchCompat.ModifyDamage(run, null, target, enemy,
                        31m, ValueProp.Move, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
                    if (actual != 31m * expected)
                        throw new Exception("Full native damage pipeline bypassed Savior Wings.");
                }
            }
            Console.WriteLine("PASS Savior Wings actual modifier dispatch, 2/3/4 recipients, 50/75 percent, stack floor, enemy-only attacks and competing prefix compatibility.");
        }
        finally { h.UnpatchAll(h.Id); }
    }

    public static bool OtherMod(ref decimal __result)
    {
        if (!_shortCircuit) return true;
        __result = 1m; return false;
    }
}

public class SaviorRunStateFixture : DispatchProxy
{
    public IEnumerable<AbstractModel> Listeners = Array.Empty<AbstractModel>();
    protected override object? Invoke(MethodInfo? method, object?[]? args) =>
        method?.Name == nameof(IRunState.IterateHookListeners) ? Listeners
            : throw new NotSupportedException(method?.Name);
}
