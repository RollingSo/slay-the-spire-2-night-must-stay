#nullable enable
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Native SFX only; presentation clock, never combat RNG or delayed tasks.</summary>
public static class DuchessAudio
{
    public readonly record struct Cue(float At, string File, float Volume);
    private static readonly Dictionary<string, ulong> LastPlayed = new();

    public static Cue[] AttackCues(DuchessAttackVfx.Kind kind) => kind switch
    {
        DuchessAttackVfx.Kind.Clock => new[] {
            new Cue(0, "glass_orb_channel.mp3", .35f),
            new Cue(.48f, "glass_orb_evoke.mp3", .6f) },
        DuchessAttackVfx.Kind.Slicer => Sword("slash_attack.mp3", .5f),
        DuchessAttackVfx.Kind.GreatCaria or DuchessAttackVfx.Kind.Greatsword => Sword("heavy_attack.mp3", .6f),
        DuchessAttackVfx.Kind.Piercer => Sword("dagger_throw.mp3", .6f),
        DuchessAttackVfx.Kind.DeathBlade => Flight("dagger_throw.mp3", "dark_orb_evoke.mp3", .55f),
        DuchessAttackVfx.Kind.GoldenBlade => Flight("dagger_throw.mp3", "glass_orb_evoke.mp3", .55f),
        DuchessAttackVfx.Kind.Miquella => Flight("glass_orb_channel.mp3", "glass_orb_passive.mp3", .45f),
        DuchessAttackVfx.Kind.Sacred => Flight("plasma_orb_channel.mp3", "plasma_orb_evoke.mp3", .45f),
        DuchessAttackVfx.Kind.Greatbow or DuchessAttackVfx.Kind.Mastery =>
            Flight("dagger_throw.mp3", "frost_orb_evoke.mp3", .6f),
        _ => Flight("frost_orb_channel.mp3", "glass_orb_passive.mp3", .5f)
    };

    private static Cue[] Sword(string hit, float volume) => new[] {
        new Cue(.02f, "slash_attack.mp3", .3f), new Cue(.3f, hit, volume) };
    private static Cue[] Flight(string launch, string hit, float volume) => new[] {
        new Cue(.04f, launch, .4f), new Cue(.36f, hit, volume) };

    public static void Play(string file, float volume, bool combat = true)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive
            || (combat && CombatManager.Instance.IsEnding)) return;
        var audio = NDebugAudioManager.Instance;
        if (audio == null) return;
        ulong now = Time.GetTicksMsec();
        // Collapse simultaneous AOE cues by sample; allow later multihit cues.
        if (LastPlayed.TryGetValue(file, out ulong previous) && now - previous < 75) return;
        LastPlayed[file] = now;
        audio.Play(file, volume);
    }
}
