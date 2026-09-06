#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Compatibility entry points. Production drawing and audio live in each character's effect suite.</summary>
public partial class NightreignHitVfx : Node2D
{
    public static Node2D? CreateIroneyeShot(Creature attacker, Creature target) =>
        IroneyeAttackEffects.Create(target, IroneyeAttackVfx.Kind.Shot, attacker);

    public static Node2D? CreateIroneyeKnife(Creature target) =>
        IroneyeAttackEffects.Create(target, IroneyeAttackVfx.Kind.Knife);

    public static Node2D? CreateIroneyeMarkTrigger(Creature target) =>
        IroneyeAttackEffects.Create(target, IroneyeAttackVfx.Kind.MarkTrigger);

    public static void PlayIroneyeKnife(Creature target) =>
        IroneyeAttackEffects.Play(target, IroneyeAttackVfx.Kind.Knife);

    public static void PlayIroneyeMarkTrigger(Creature target) =>
        IroneyeAttackEffects.Play(target, IroneyeAttackVfx.Kind.MarkTrigger);

    public static Node2D? CreateGuardianWhirlwind(Creature target) =>
        GuardianAttackEffects.Create(target, GuardianAttackVfx.Kind.Whirlwind);

    public static Node2D? CreateGuardianCounter(Creature target) =>
        GuardianAttackEffects.Create(target, GuardianAttackVfx.Kind.Counter);

    public static Node2D? CreateGuardianShieldPoke(Creature target) =>
        GuardianAttackEffects.Create(target, GuardianAttackVfx.Kind.Weapon);

    public static void PlayGuardianWhirlwind(Creature target) =>
        GuardianAttackEffects.Play(target, GuardianAttackVfx.Kind.Whirlwind);

    public static void PlayGuardianCounter(Creature target) =>
        GuardianAttackEffects.Play(target, GuardianAttackVfx.Kind.Counter);
}
