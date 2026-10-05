#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>A short completion cue, shared by direct and card-assisted charging.</summary>
public partial class RevenantChargeVfx : ParticleAttackVfx
{
    public const string CompletionSound = "dark_orb_evoke.mp3";
    public override float Duration => .65f;
    protected override int EffectIndex => 22;

    public static void Play(Creature owner)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || owner == null || owner.IsDead
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding) return;
        var node = owner.GetCreatureNode();
        var container = owner.GetVfxContainer();
        if (node == null || container == null) return;
        container.AddChildSafely(new RevenantChargeVfx {
            GlobalPosition = node.VfxSpawnPosition, ZIndex = 20,
            OnStart = () => NDebugAudioManager.Instance?.Play(CompletionSound, .55f)
        });
    }
}
