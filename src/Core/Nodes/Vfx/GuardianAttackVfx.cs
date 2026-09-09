#nullable enable
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;
public partial class GuardianAttackVfx : ParticleAttackVfx
{
    public enum Kind { Weapon, Whirlwind, Counter }
    public Kind AttackKind { get; set; }
    protected override int EffectIndex => (int)AttackKind;
    public float VisualScale => AttackKind == Kind.Counter ? AttackVfxSizing.CounterScale(VisualDamage) / 1.65f : 1f;
    protected override float PowerScale => VisualScale;
    public override float Duration => AttackKind == Kind.Whirlwind ? 1.12f : AttackKind == Kind.Counter ? 1.1f : .92f;
}
