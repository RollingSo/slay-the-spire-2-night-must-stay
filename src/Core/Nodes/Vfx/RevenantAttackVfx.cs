#nullable enable
using System;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;
public partial class RevenantAttackVfx : ParticleAttackVfx
{
    public enum Kind { HaloOut, HaloReturn, LightningRed, LightningYellow, LightningBlue,
        BeastRock, BeastClaw, Frenzy, CursedClaw, Helen, Frederick, Sebastian, Heal }
    public Kind AttackKind { get; set; }
    public Vector2 Source { get; set; } = new(-280,0);
    protected override int EffectIndex => 9 + (int)AttackKind;
    protected override Vector2 Origin => Source;
    protected override Vector2 Route(float t) => FlightPoint(t);
    public float VisualScale => AttackKind is Kind.HaloOut or Kind.HaloReturn ? AttackVfxSizing.HaloScale(VisualDamage)/1.6f : 1f;
    protected override float PowerScale => VisualScale;
    public override float Duration => AttackKind switch {
        Kind.HaloOut => 1.05f, Kind.HaloReturn => 1.1f, Kind.Heal => 1.4f,
        Kind.Frenzy => 1.25f, _ => 1.12f
    };
    public Vector2 FlightPoint(float t)
    {
        t=Math.Clamp(t,0,1); Vector2 p=Source.Lerp(Vector2.Zero,t);
        if(AttackKind==Kind.BeastRock)p.Y-=80*4*t*(1-t);
        if(AttackKind==Kind.HaloReturn)p.Y-=38*MathF.Sin(t*MathF.PI);
        return p;
    }
}
