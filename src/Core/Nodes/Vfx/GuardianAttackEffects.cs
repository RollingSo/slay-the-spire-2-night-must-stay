#nullable enable
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

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

    public static AttackCommand WithGuardianWindFx(this AttackCommand command, GuardianWindVfx.Kind kind) =>
        command.WithHitFx().WithHitVfxNode(target => CreateWind(target, kind));

    public static AttackCommand WithGuardianStormFx(this AttackCommand command, GuardianStormAttackVfx.Kind kind) =>
        command.WithHitFx().WithHitVfxNode(target => CreateStorm(target, kind));

    public static Node2D? CreateStorm(Creature target, GuardianStormAttackVfx.Kind kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target.IsDead
            || target.GetCreatureNode() is not { } node) return null;
        return new GuardianStormAttackVfx { AttackKind = kind, GlobalPosition = node.VfxSpawnPosition,
            ZIndex = 20, OnStart = () => PlaySound(GuardianAttackVfx.Kind.Whirlwind) };
    }

    public static AttackCommand WithGuardianSpecialFx(this AttackCommand command, GuardianSpecialAttackVfx.Kind kind) =>
        command.WithHitFx().WithHitVfxNode(target => CreateSpecial(target, kind));

    public static Node2D? CreateSpecial(Creature target, GuardianSpecialAttackVfx.Kind kind, bool echoOnly = false)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target.IsDead
            || target.GetCreatureNode() is not { } node) return null;
        var accent = new GuardianSpecialAttackVfx { AttackKind = kind, EchoOnly = echoOnly,
            GlobalPosition = node.VfxSpawnPosition, ZIndex = 20,
            OnStart = () => PlaySound(kind == GuardianSpecialAttackVfx.Kind.Topple
                ? GuardianAttackVfx.Kind.Whirlwind : kind == GuardianSpecialAttackVfx.Kind.Heavenfall
                    ? GuardianAttackVfx.Kind.Counter : GuardianAttackVfx.Kind.Weapon) };
        if (kind != GuardianSpecialAttackVfx.Kind.Heavenfall) return accent;
        // Reuse the original production center/ground bursts as well as our cyan/gold accents.
        var combined = new Node2D { ZIndex = 20 };
        if (NGrandFinaleImpactVfx.Create(target) is { } original) combined.AddChild(original);
        combined.AddChild(accent);
        return new GuardianHeavenfallImpactContainer { Effect = combined };
    }

    public static async Task PlayHeavenfallPrelude(Creature owner)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding
            || NCombatRoom.Instance?.CombatVfxContainer is not { } container
            || owner.GetCreatureNode() is not { } node) return;
        if (NGrandFinaleVfx.Create(owner) is not { } original) return;
        container.AddChildSafely(original);
        container.AddChildSafely(new GuardianHeavenfallPreludeVfx {
            GlobalPosition = node.VfxSpawnPosition, ZIndex = 27 });
        await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration, false);
    }

    public static void PlaySpecial(Creature target, GuardianSpecialAttackVfx.Kind kind, bool echoOnly = false)
    {
        if (target.GetVfxContainer() is { } container && CreateSpecial(target, kind, echoOnly) is { } effect)
            container.AddChildSafely(effect);
    }

    public static Node2D? CreateWind(Creature target, GuardianWindVfx.Kind kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target.IsDead
            || target.GetCreatureNode() is not { } node) return null;
        return new GuardianWindVfx { WindKind = kind, GlobalPosition = node.VfxSpawnPosition,
            ZIndex = 20, OnStart = () => PlaySound(GuardianAttackVfx.Kind.Whirlwind) };
    }

    public static void PlayWind(Creature target, GuardianWindVfx.Kind kind)
    {
        if (target.GetVfxContainer() is { } container && CreateWind(target, kind) is { } effect)
            container.AddChildSafely(effect);
    }

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
