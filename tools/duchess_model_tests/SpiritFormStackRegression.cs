using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

internal static class SpiritFormStackRegression
{
    private static readonly List<string> Events = new();
    public static void Run()
    {
        var h = new HarmonyLib.Harmony("tests.spirit-form-stacking");
        h.Patch(AccessTools.Method(typeof(RevenantSummonManager), "IncreaseSummonsMaxAndCurrentHp"),
            prefix: new HarmonyMethod(typeof(SpiritFormStackRegression), nameof(Hp)));
        h.Patch(AccessTools.Method(typeof(RevenantSummonManager), "TriggerResonance"),
            prefix: new HarmonyMethod(typeof(SpiritFormStackRegression), nameof(Resonate)));
        var player = TrackingArrowRegression.MakePlayer();
        var owner = new Creature(player, 70, 70);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, owner);
        try
        {
            var power = ModelDb.Power<SpiritFormPower>().ToMutable();
            typeof(PowerModel).GetProperty("Owner")!.SetValue(power, owner);
            if (power.StackType != MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter) throw new Exception("Must stack");
            for (int stacks = 1; stacks <= 3; stacks++)
            {
                power.SetAmount(stacks, false);
                Events.Clear();
                power.AfterPlayerTurnStartLate(null!, TrackingArrowRegression.MakePlayer()).GetAwaiter().GetResult();
                if (Events.Count != 0) throw new Exception("Triggered on teammate turn");
                power.AfterPlayerTurnStartLate(null!, player).GetAwaiter().GetResult();
                if (!Events.SequenceEqual(Enumerable.Range(0, stacks).SelectMany(_ => new[] { "hp6", "resonate" })))
                    throw new Exception("Each stack must increase HP then resonate once");
            }
            Console.WriteLine("PASS: Spirit Form 1/2/3 stacks and teammate turn isolation (summon effects stubbed).");
        }
        finally { h.UnpatchAll(h.Id); RevenantSummonManager.Clear(player); }
    }
    private static bool Hp(int amount, ref Task __result) { Events.Add("hp" + amount); __result = Task.CompletedTask; return false; }
    private static bool Resonate(ref Task __result) { Events.Add("resonate"); __result = Task.CompletedTask; return false; }
}
