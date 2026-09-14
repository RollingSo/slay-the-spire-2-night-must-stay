using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace NightMustStay.Core.Patches;

/// <summary>
/// Player-side revived gardeners skip enemy-only room initialization and therefore
/// have no SkittishPower. Keep their original animations without requiring that power.
/// </summary>
[HarmonyPatch(typeof(PhantasmalGardener), nameof(PhantasmalGardener.GenerateAnimator))]
public static class RevenantGardenerAnimatorPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PhantasmalGardener __instance,
        MegaSprite controller,
        ref CreatureAnimator __result)
    {
        if (__instance.Creature?.Side != CombatSide.Player)
            return true;

        var idle = new AnimState("idle_loop", isLooping: true);
        var cast = new AnimState("buff") { NextState = idle };
        var attack = new AnimState("attack") { NextState = idle };
        var multiAttack = new AnimState("attack_multi") { NextState = idle };
        var hit = new AnimState("hurt_extended") { NextState = idle };
        var blockIdle = new AnimState("block_loop", isLooping: true);
        var blockHit = new AnimState("hurt") { NextState = blockIdle };
        var blockStart = new AnimState("block_start") { NextState = blockIdle };
        var blockEnd = new AnimState("block_end") { NextState = idle };

        bool HasSkittishBlock() =>
            __instance.Creature.GetPower<SkittishPower>()?.HasGainedBlockThisTurn == true;

        __result = new CreatureAnimator(idle, controller);
        __result.AddAnyState("Idle", idle);
        __result.AddAnyState("Cast", cast);
        __result.AddAnyState("Attack", attack);
        __result.AddAnyState("AttackMulti", multiAttack);
        __result.AddAnyState("Dead", new AnimState("die"));
        __result.AddAnyState("Hit", hit, () => !HasSkittishBlock());
        __result.AddAnyState("Hit", blockHit, HasSkittishBlock);
        __result.AddAnyState("BlockStart", blockStart);
        __result.AddAnyState("BlockEnd", blockEnd);
        return false;
    }
}
