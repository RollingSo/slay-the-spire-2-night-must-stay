#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public static class IroneyeAttackEffects
{
    public const string KnifeSfx = "slash_attack.mp3";
    // Native projectile whoosh, not a claim to reproduce Nightreign's bow recording.
    public const string ShotSfx = "dagger_throw.mp3";
    public const string MarkSfx = "glass_orb_passive.mp3";
    public const string PoisonBurstSfx = "dark_orb_evoke.mp3";
    private static readonly ulong[] LastSoundAt = new ulong[6];
    private static readonly bool[] HasPlayed = new bool[6];

    public static AttackCommand WithIroneyeKnifeFx(this AttackCommand command, bool poisoned = false) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, poisoned ? IroneyeAttackVfx.Kind.PoisonKnife : IroneyeAttackVfx.Kind.Knife));

    public static AttackCommand WithIroneyeShotFx(this AttackCommand command, Creature attacker, bool poisoned = false) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, poisoned ? IroneyeAttackVfx.Kind.PoisonShot : IroneyeAttackVfx.Kind.Shot, attacker));

    public static Node2D? Create(Creature target, IroneyeAttackVfx.Kind kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return null;
        var node = target.GetCreatureNode();
        if (node == null) return null;
        Vector2 origin = Vector2.Zero;
        if (kind is IroneyeAttackVfx.Kind.Shot or IroneyeAttackVfx.Kind.PoisonShot)
        {
            var source = attacker?.GetCreatureNode();
            if (source == null) return null;
            origin = source.VfxSpawnPosition - node.VfxSpawnPosition;
        }
        return new IroneyeAttackVfx
        {
            AttackKind = kind,
            ShotOrigin = origin,
            GlobalPosition = node.VfxSpawnPosition,
            ZIndex = kind == IroneyeAttackVfx.Kind.MarkTrigger ? 60 : 20,
            OnStart = () => PlaySound(kind),
        };
    }

    public static void Play(Creature target, IroneyeAttackVfx.Kind kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return;
        var container = target.GetVfxContainer();
        if (container == null) return;
        var effect = Create(target, kind, attacker);
        if (effect != null) container.AddChildSafely(effect);
    }

    private static void PlaySound(IroneyeAttackVfx.Kind kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding) return;
        var audio = NDebugAudioManager.Instance;
        if (audio == null) return;
        ulong now = Time.GetTicksMsec();
        int index = (int)(kind switch { IroneyeAttackVfx.Kind.PoisonKnife => IroneyeAttackVfx.Kind.Knife,
            IroneyeAttackVfx.Kind.PoisonShot => IroneyeAttackVfx.Kind.Shot, _ => kind });
        // Collapse simultaneous AOE sounds; separate types never suppress one another.
        ulong interval = kind == IroneyeAttackVfx.Kind.PoisonBurst ? 180UL : kind == IroneyeAttackVfx.Kind.MarkTrigger ? 160UL : 65UL;
        if (HasPlayed[index] && now - LastSoundAt[index] < interval) return;
        HasPlayed[index] = true;
        LastSoundAt[index] = now;
        switch (kind)
        {
            case IroneyeAttackVfx.Kind.PoisonKnife:
            case IroneyeAttackVfx.Kind.Knife: audio.Play(KnifeSfx, 0.6f); break;
            case IroneyeAttackVfx.Kind.PoisonShot:
            case IroneyeAttackVfx.Kind.Shot: audio.Play(ShotSfx, 0.7f); break;
            case IroneyeAttackVfx.Kind.MarkTrigger: audio.Play(MarkSfx, 0.65f); break;
            case IroneyeAttackVfx.Kind.PoisonBurst: audio.Play(PoisonBurstSfx, 0.65f); break;
        }
    }
}
