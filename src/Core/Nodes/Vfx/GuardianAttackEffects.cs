#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Nodes.Vfx;

public static class GuardianAttackEffects
{
    public const string WeaponSfx = "slash_attack.mp3";
    public const string WindSfx = "event:/sfx/characters/ironclad/ironclad_whirlwind";
    // Same shipped SFX used by Bludgeon, through the game's SFX bus/pool.
    public const string CounterSfx = "heavy_attack.mp3";
    private static readonly ulong[] LastSoundAt = new ulong[3];
    private static readonly bool[] HasPlayed = new bool[3];

    public static AttackCommand WithGuardianWeaponFx(this AttackCommand command) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, GuardianAttackVfx.Kind.Weapon));

    public static AttackCommand WithGuardianWhirlwindFx(this AttackCommand command) =>
        command.WithHitFx().WithHitVfxNode(target => Create(target, GuardianAttackVfx.Kind.Whirlwind));

    public static Node2D? Create(Creature target, GuardianAttackVfx.Kind kind, decimal visualDamage = 8m)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return null;
        var node = target.GetCreatureNode();
        if (node == null) return null;
        return new GuardianAttackVfx
        {
            AttackKind = kind,
            VisualDamage = visualDamage,
            GlobalPosition = node.VfxSpawnPosition,
            ZIndex = 20,
            OnStart = () => PlaySound(kind),
        };
    }

    public static void Play(Creature target, GuardianAttackVfx.Kind kind, decimal visualDamage = 8m)
    {
        if (TestMode.IsOn || target == null || target.IsDead) return;
        var container = target?.GetVfxContainer();
        if (container == null) return;
        var effect = Create(target!, kind, visualDamage);
        if (effect != null) container.AddChildSafely(effect);
    }

    public static void PlayCounter(Creature target, Creature dealer, decimal amount)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target == null || target.IsDead
            || target.GetCreatureNode() == null || target.GetVfxContainer() == null) return;
        Play(target, GuardianAttackVfx.Kind.Counter, AttackVfxDamage.Preview(target, dealer, amount, ValueProp.Unpowered));
    }

    private static void PlaySound(GuardianAttackVfx.Kind kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding) return;
        ulong now = Time.GetTicksMsec();
        int index = (int)kind;
        // AOE factories run once per enemy. Collapse that burst, not the visuals.
        // Wind's longer sample gets a longer gate to avoid piling up on X/multihit cards.
        ulong interval = kind == GuardianAttackVfx.Kind.Whirlwind ? 400UL : 75UL;
        if (HasPlayed[index] && now - LastSoundAt[index] < interval) return;
        HasPlayed[index] = true;
        LastSoundAt[index] = now;
        switch (kind)
        {
            case GuardianAttackVfx.Kind.Weapon: NDebugAudioManager.Instance?.Play(WeaponSfx, 0.75f); break;
            case GuardianAttackVfx.Kind.Whirlwind: SfxCmd.Play(WindSfx, 0.65f); break;
            case GuardianAttackVfx.Kind.Counter: NDebugAudioManager.Instance?.Play(CounterSfx, 0.85f); break;
        }
    }
}
