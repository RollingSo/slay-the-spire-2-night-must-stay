using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Patches;

try
{
typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
var harmony = new Harmony("NightMustStay.SummonSynthesis.Tests");
try
{
    // Replace only the Godot/Spine constructor boundary. GenerateAnimator,
    // transition registration, and native trigger predicates execute normally.
    harmony.Patch(typeof(CreatureAnimator).GetConstructors().Single(),
        prefix: new HarmonyMethod(typeof(AnimatorFixture), nameof(AnimatorFixture.Construct)));
    harmony.CreateClassProcessor(typeof(RevenantGardenerAnimatorPatch)).Patch();
    foreach (CombatSide side in new[] { CombatSide.Player, CombatSide.Enemy })
    {
        var gardener = Mutable(new PhantasmalGardener());
        var creature = new Creature(gardener, side, null);
        if (side == CombatSide.Player)
        {
            await creature.AfterAddedToRoom();
            Check(creature.GetPower<SkittishPower>() == null, "Revived gardener gained an enemy-only power.");
        }
        var power = (SkittishPower)new SkittishPower().ToMutable();
        var powers = (List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!;
        if (side == CombatSide.Enemy) powers.Add(power);
        CreatureAnimator animator = gardener.GenerateAnimator(null!);
        var transitions = (AnimState)AccessTools.Field(typeof(CreatureAnimator), "_anyState").GetValue(animator)!;
        Check(transitions.CallTrigger("Hit")?.Id == "hurt_extended", "Unblocked Hit is not safe.");
        foreach (var (trigger, state) in new[] {
            ("Idle", "idle_loop"), ("Attack", "attack"), ("AttackMulti", "attack_multi"),
            ("Cast", "buff"), ("Dead", "die"), ("BlockStart", "block_start"), ("BlockEnd", "block_end") })
            Check(transitions.CallTrigger(trigger)?.Id == state, $"Lost {trigger} animation.");
        Check(transitions.CallTrigger("Hit")?.NextState?.Id == "idle_loop", "Hit does not return to idle.");
        if (side == CombatSide.Player) powers.Add(power);
        AccessTools.Property(typeof(SkittishPower), "HasGainedBlockThisTurn").SetValue(power, true);
        Check(transitions.CallTrigger("Hit")?.Id == "hurt", "Existing Skittish block behavior changed.");
        powers.Remove(power);
        if (side == CombatSide.Player)
            Check(transitions.CallTrigger("Hit")?.Id == "hurt_extended", "Power removal reintroduced the null reference.");
    }
    Console.WriteLine("PASS: actual gardener animator patch; player-side missing/removed power; enemy behavior; hit/attack/cast/death transitions. Spine rendering is stubbed.");
}
finally { harmony.UnpatchAll(harmony.Id); }

await SynthesisChecks.Run();
return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

static T Mutable<T>(T model) where T : AbstractModel
{
    AccessTools.Method(typeof(AbstractModel), "NeverEverCallThisOutsideOfTests_SetIsMutable").Invoke(model, [true]);
    return model;
}
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

public static class AnimatorFixture
{
    public static bool Construct(CreatureAnimator __instance, AnimState initialState)
    {
        AccessTools.Field(typeof(CreatureAnimator), "_anyState").SetValue(__instance, new AnimState("anyState"));
        AccessTools.Field(typeof(CreatureAnimator), "_currentState").SetValue(__instance, initialState);
        return false;
    }
}
