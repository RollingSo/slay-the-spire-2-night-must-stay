#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

public static class WylderAttackEffects
{
    public const string SwordSfx = "slash_attack.mp3";
    public const string HeavySfx = "heavy_attack.mp3";
    public const string ClawSfx = "dagger_throw.mp3";
    public const string FlameSfx = "STS_SFX_BurnCard_v1.mp3";
    private static readonly ulong[] LastSoundAt = new ulong[14];
    private static readonly bool[] HasPlayed = new bool[14];

    public static AttackCommand WithWylderFx(this AttackCommand command, WylderAttackVfx.Kind kind, Creature attacker) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, kind, attacker));

    public static AttackCommand WithEndTheNightFx(this AttackCommand command) =>
        command.WithHitFx().WithHitVfxNode(WylderEndTheNightVfx.CreateImpact);

    public static Node2D? Create(Creature target, WylderAttackVfx.Kind kind, Creature attacker)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return null;
        var targetNode = target.GetCreatureNode();
        if (targetNode == null) return null;
        Vector2 origin = new(-260f, 0f);
        if (attacker?.GetCreatureNode() is { } source)
            origin = source.VfxSpawnPosition - targetNode.VfxSpawnPosition;
        return new WylderAttackVfx
        {
            Name = $"Wylder{kind}Vfx",
            AttackKind = kind,
            Origin = origin,
            GlobalPosition = targetNode.VfxSpawnPosition,
            ZIndex = 24,
            OnStart = () => PlaySound(kind),
        };
    }

    public static void Play(Creature target, WylderAttackVfx.Kind kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return;
        if (target.GetVfxContainer() is not { } container) return;
        if (Create(target, kind, attacker ?? target) is { } effect) container.AddChildSafely(effect);
    }

    private static void PlaySound(WylderAttackVfx.Kind kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding) return;
        var audio = NDebugAudioManager.Instance;
        if (audio == null) return;
        ulong now = Time.GetTicksMsec();
        int index = (int)kind;
        ulong interval = kind is WylderAttackVfx.Kind.FireExplosion or WylderAttackVfx.Kind.TakersFlames
            or WylderAttackVfx.Kind.GhostflameExplosion ? 180UL : 75UL;
        if (HasPlayed[index] && now - LastSoundAt[index] < interval) return;
        HasPlayed[index] = true;
        LastSoundAt[index] = now;
        string sample = kind switch
        {
            WylderAttackVfx.Kind.Claw => ClawSfx,
            WylderAttackVfx.Kind.FireExplosion or WylderAttackVfx.Kind.FlameSlash
                or WylderAttackVfx.Kind.TakersFlames or WylderAttackVfx.Kind.GhostflameExplosion => FlameSfx,
            WylderAttackVfx.Kind.BucklerImpact or WylderAttackVfx.Kind.Execution
                or WylderAttackVfx.Kind.OrdovisVortex or WylderAttackVfx.Kind.SiluriaVortex => HeavySfx,
            _ => SwordSfx,
        };
        audio.Play(sample, kind is WylderAttackVfx.Kind.Execution or WylderAttackVfx.Kind.BucklerImpact ? .85f : .7f);
    }
}
