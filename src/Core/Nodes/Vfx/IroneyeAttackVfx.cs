#nullable enable
using System;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;
public partial class IroneyeAttackVfx : ParticleAttackVfx
{
    public enum Kind { Knife, Shot, MarkTrigger, PoisonKnife, PoisonShot, PoisonBurst }
    public Kind AttackKind { get; set; }
    public Vector2 ShotOrigin { get; set; } = new(-280,0);
    public const float FlightSeconds = .16f;
    protected override int EffectIndex => 3 + (int)AttackKind;
    protected override Vector2 Origin => ShotOrigin;
    protected override Vector2 Route(float t) => ArrowTipAt(t);
    public float VisualScale => 1f;
    public override float Duration => AttackKind == Kind.PoisonBurst ? 1.35f : AttackKind == Kind.MarkTrigger ? 1.08f : .92f;
    public Vector2 ArrowTipAt(float t) => ShotOrigin.Lerp(Vector2.Zero, Ease(0,1,Math.Clamp(t*Duration/FlightSeconds,0,1)));
}
