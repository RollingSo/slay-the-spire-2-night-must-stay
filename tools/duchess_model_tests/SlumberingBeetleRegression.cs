using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using NightMustStay.Core.Models.Cards;

internal static class SlumberingBeetleRegression
{
    private static int _wakeCalls;

    public static void Run()
    {
        if (!ModelDb.Contains(typeof(SlumberingBeetle)))
            typeof(ModelDb).GetMethod("Inject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { typeof(SlumberingBeetle) });
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.beetle-stun");
        // Only audiovisual wake-up work is stubbed. Native stun admission,
        // mandatory execution, state transitions and the mod prefix are real.
        harmony.Patch(AccessTools.Method(typeof(SlumberingBeetle), nameof(SlumberingBeetle.WakeUpMove)),
            prefix: new HarmonyMethod(typeof(SlumberingBeetleRegression), nameof(Wake)));
        try
        {
            foreach (bool patched in new[] { false, true })
            {
            if (patched)
                harmony.CreateClassProcessor(typeof(Fearless).Assembly.GetType(
                    "NightMustStay.Core.Patches.SlumberingBeetleStunPatch")!).Patch();
            foreach (bool sleeping in new[] { false, true })
            foreach (bool alreadyAwake in new[] { false, true })
            {
                _wakeCalls = 0;
                var beetle = (SlumberingBeetle)ModelDb.Monster<SlumberingBeetle>().MutableClone();
                var creature = new Creature(TrackingArrowRegression.MakePlayer(), 86, 86)
                    { CombatState = new CombatState() };
                AccessTools.Field(typeof(Creature), "<Monster>k__BackingField").SetValue(creature, beetle);
                AccessTools.PropertySetter(typeof(MonsterModel), nameof(MonsterModel.Creature)).Invoke(beetle, new object[] { creature });
                beetle.IsAwake = alreadyAwake;
                var snore = new MoveState("SNORE_MOVE", _ => Task.CompletedTask);
                var rollout = new MoveState(SlumberingBeetle.rolloutMoveId, _ => Task.CompletedTask);
                snore.FollowUpState = rollout;
                rollout.FollowUpState = rollout;
                var machine = new MonsterMoveStateMachine(new MonsterState[] { snore, rollout }, snore);
                AccessTools.Field(typeof(MonsterModel), "_moveStateMachine").SetValue(beetle, machine);
                AccessTools.PropertySetter(typeof(MonsterModel), nameof(MonsterModel.NextMove)).Invoke(beetle, new object[] { snore });
                var powers = (List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!;
                var slumber = ModelDb.Power<SlumberPower>().ToMutable();
                powers.Add(slumber);
                // Imbalance claims the mandatory stun first; Slumber expires
                // afterward and its native wake stun is refused.
                creature.StunInternal(_ => Task.CompletedTask, null);
                var stun = beetle.NextMove;
                if (!sleeping) powers.Clear();
                creature.StunInternal(beetle.WakeUpMove, SlumberingBeetle.rolloutMoveId);
                if (!ReferenceEquals(stun, beetle.NextMove))
                    throw new Exception("Fixture must reproduce native rejection of a second stun.");
                stun.PerformMove(Array.Empty<Creature>()).GetAwaiter().GetResult();
                int expected = patched && !sleeping && !alreadyAwake ? 1 : 0;
                if (_wakeCalls != expected || beetle.IsAwake != (alreadyAwake || expected == 1))
                    throw new Exception("Beetle wake-up guard mismatch.");
                if (expected == 1 && stun.GetNextState(creature, null!) != SlumberingBeetle.rolloutMoveId)
                    throw new Exception("Recovered beetle must proceed to Rollout, not Snore.");
            }
            }
            Console.WriteLine("PASS: reproduced native lost wake-up before patch; patched stun wakes exactly once and resumes Rollout; intact sleep and awake beetles unaffected.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static bool Wake(SlumberingBeetle __instance, ref Task __result)
    {
        _wakeCalls++;
        __instance.IsAwake = true;
        __result = Task.CompletedTask;
        return false;
    }
}
