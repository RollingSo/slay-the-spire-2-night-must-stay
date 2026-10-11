using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace NightMustStay.Core.Patches;

// Pets skip enemy room initialization. Do not add IllusionPower merely to
// satisfy the animator: that would import the enemy's revival mechanics.
[HarmonyPatch(typeof(Parafright), nameof(Parafright.GenerateAnimator))]
public static class RevenantParafrightAnimatorPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Parafright __instance, MegaSprite controller, ref CreatureAnimator __result)
    {
        if (__instance.Creature?.Side != CombatSide.Player)
            return true;

        var idle = new AnimState("idle_loop", isLooping: true);
        var spawn = new AnimState("spawn") { NextState = idle };
        var attack = new AnimState("attack") { NextState = idle };
        var hurt = new AnimState("hurt") { NextState = idle };
        __result = new CreatureAnimator(spawn, controller);
        __result.AddAnyState("Idle", idle);
        __result.AddAnyState("Attack", attack);
        __result.AddAnyState("Hit", hurt);
        __result.AddAnyState("Dead", new AnimState("die"));
        return false;
    }
}

// These two monsters are parts of the boss background, not standalone pets.
// Their HP/death callbacks must never animate the original encounter's arms.
[HarmonyPatch]
public static class RevenantCrabBackgroundHookPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var type in new[] { typeof(Crusher), typeof(Rocket) })
        {
            yield return AccessTools.DeclaredMethod(type, nameof(MonsterModel.AfterCurrentHpChanged));
            yield return AccessTools.DeclaredMethod(type, nameof(MonsterModel.BeforeDeath));
        }
    }

    [HarmonyPrefix]
    private static bool Prefix(MonsterModel __instance, ref Task __result)
    {
        if (__instance.Creature?.Side != CombatSide.Player)
            return true;
        __result = Task.CompletedTask;
        return false;
    }
}

// Native boss listeners identify their minions by monster type alone. A revived
// copy on the other side must not count as the boss losing its own minion.
[HarmonyPatch]
public static class RevenantBossMinionDeathHookPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(KinPriest), nameof(KinPriest.AfterDeath));
        yield return AccessTools.DeclaredMethod(typeof(Queen), nameof(Queen.AfterDeath));
    }

    public static bool ShouldRun(CombatSide listenerSide, CombatSide deceasedSide) =>
        listenerSide == CombatSide.Enemy && deceasedSide == CombatSide.Enemy;

    [HarmonyPrefix]
    private static bool Prefix(MonsterModel __instance, Creature creature, ref Task __result)
    {
        if (ShouldRun(__instance.Creature.Side, creature.Side))
            return true;
        __result = Task.CompletedTask;
        return false;
    }
}
