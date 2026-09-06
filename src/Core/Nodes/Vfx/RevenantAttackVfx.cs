#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Deterministic, texture-free prayer and family signatures; never consumes combat RNG.</summary>
public partial class RevenantAttackVfx : Node2D
{
    public enum Kind { HaloOut, HaloReturn, LightningRed, LightningYellow, LightningBlue,
        BeastRock, BeastClaw, Frenzy, CursedClaw, Helen, Frederick, Sebastian, Heal }
    public Kind AttackKind { get; set; }
    // Destination is this node's origin. HaloOut's destination is beyond the enemy.
    public Vector2 Source { get; set; } = new(-280, 0);
    public Action? OnStart { get; set; }
    public float Duration => AttackKind switch
    {
        Kind.HaloOut => .52f, Kind.HaloReturn => .6f, Kind.Heal => .85f,
        Kind.BeastRock => .48f, Kind.Frenzy => .5f, Kind.Frederick => .44f, _ => .38f
    };
    private float _age;
    private static readonly Color Gold = new("#FFD04D"), Cream = new("#FFF2B0"),
        Orange = new("#F17D27"), Red = new("#EE3956"), Blue = new("#62BFFF"),
        Violet = new("#9572D4"), Ghost = new("#A0DDDD"), Bone = new("#E7E0C5"),
        Earth = new("#A7875E"), Ink = new("#202131");

    public override void _Ready() { OnStart?.Invoke(); OnStart = null; QueueRedraw(); }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Duration) QueueFree(); else QueueRedraw();
    }
    public override void _Draw() => DrawFrame(Math.Clamp(_age / Duration, 0, 1));

    public Vector2 FlightPoint(float t)
    {
        t = Math.Clamp(t, 0, 1);
        Vector2 p = Source.Lerp(Vector2.Zero, t);
        if (AttackKind == Kind.BeastRock) p.Y -= 65 * 4 * t * (1 - t);
        if (AttackKind == Kind.HaloReturn) p.Y -= 28 * MathF.Sin(t * MathF.PI);
        return p;
    }

    public void DrawFrame(float t)
    {
        t = Math.Clamp(t, 0, 1);
        float a = 1 - Smooth(.63f, 1, t);
        if (a <= 0) return;
        switch (AttackKind)
        {
            case Kind.HaloOut: case Kind.HaloReturn: Halo(t); break;
            case Kind.LightningRed: case Kind.LightningYellow: case Kind.LightningBlue: Lightning(t, a); break;
            case Kind.BeastRock: Rock(t, a); break;
            case Kind.BeastClaw: GroundClaw(t, a); break;
            case Kind.Frenzy: Frenzy(t, a); break;
            case Kind.CursedClaw: Claw(t, a); break;
            case Kind.Helen: Helen(t, a); break;
            case Kind.Frederick: Hammer(t, a); break;
            case Kind.Sebastian: Skeleton(t, a); break;
            case Kind.Heal: Heal(t, a); break;
        }
    }

    private void Halo(float t)
    {
        // Full outbound journey, no automatic boomerang: return is a separate next-turn node.
        Vector2 p = FlightPoint(Smooth(0, .94f, t));
        float a = Smooth(0, .05f, t) * (1 - Smooth(.94f, 1, t));
        float radius = AttackKind == Kind.HaloReturn ? 30 - 18 * Smooth(.75f, 1, t) : 32;
        Vector2 direction = Source.LengthSquared() > 1 ? -Source.Normalized() : Vector2.Right;
        for (int i = 2; i >= 0; i--)
            Ellipse(p - direction * i * 15, radius, radius * .3f, Tint(Gold, a * (1 - i * .32f)), i == 0 ? 5 : 2);
        Ellipse(p, radius - 3, radius * .3f - 2, Tint(Cream, a), 2);
        Diamond(p + new Vector2(radius, 0), 9, 3, Tint(Cream, a));
    }

    private void Lightning(float t, float a)
    {
        Color color = AttackKind == Kind.LightningRed ? Red : AttackKind == Kind.LightningBlue ? Blue : Gold;
        float flash = a * (.75f + .25f * MathF.Cos(t * 31));
        int variant = AttackKind == Kind.LightningRed ? 1 : AttackKind == Kind.LightningBlue ? -1 : 0;
        Vector2[] points = { new(-34, -215), new(13 + variant * 8, -150), new(-22, -137),
            new(28 - variant * 7, -75), new(5, -82), new(0, 10) };
        Stroke(points, Tint(color, flash * .4f), 16);
        Stroke(points, Tint(color, flash), 7);
        Stroke(points, Tint(Cream, flash), 2);
        for (int i = 0; i < 3; i++)
        {
            Vector2 root = points[i + 1];
            float sign = i % 2 == 0 ? -1 : 1;
            Stroke(new[] { root, root + new Vector2(sign * 42, 13), root + new Vector2(sign * 28, 30),
                root + new Vector2(sign * 58, 60) }, Tint(color, a * .75f), 2);
        }
        Burst(new Vector2(0, 12), t, color, a, 6);
        if (AttackKind == Kind.LightningBlue)
            for (int i = 0; i < 3; i++) Diamond(new Vector2(-32 + i * 32, 32), 5, 15 * (1 - t), Tint(Blue, a));
    }

    private void Rock(float t, float a)
    {
        float flight = Math.Clamp(t / .38f, 0, 1);
        Vector2 p = FlightPoint(flight);
        if (t < .47f)
        {
            DrawColoredPolygon(new[] { p + new Vector2(-24,-11), p + new Vector2(-7,-28), p + new Vector2(20,-19),
                p + new Vector2(28,8), p + new Vector2(5,24), p + new Vector2(-25,14) }, Tint(Ink,a));
            DrawColoredPolygon(new[] { p + new Vector2(-20,-10), p + new Vector2(-7,-24), p + new Vector2(17,-16),
                p + new Vector2(23,7), p + new Vector2(4,18), p + new Vector2(-21,11) }, Tint(Earth,a));
            Triangle(p + new Vector2(-7,-24), p + new Vector2(17,-16), p + new Vector2(-4,7), Tint(Bone,a));
        }
        if (t < .38f) return;
        float hit = (t - .38f) / .62f;
        Burst(Vector2.Zero, hit, Earth, a, 5);
        for (int i = 0; i < 4; i++)
        {
            Vector2 q = Vector2.FromAngle(-2.9f + i * .9f) * (23 + hit * 78);
            Diamond(q, 7 * (1-hit), 5 * (1-hit), Tint(Earth,a));
        }
    }

    private void GroundClaw(float t, float a)
    {
        float advance = Smooth(0,.4f,t);
        for (int i = 0; i < 3; i++)
        {
            Vector2 p = new(-75 + advance * 100, 32 + i * 16);
            Vector2[] ridge = new Vector2[19];
            for (int j=0;j<ridge.Length;j++)
            {
                float u=j/18f;
                ridge[j]=p+new Vector2(-110+150*u,-MathF.Sin(u*MathF.PI)*(65+i*6));
            }
            Ribbon(ridge,14,Tint(Ink,a));
            Ribbon(ridge,10,Tint(Earth,a));
            Ribbon(ridge,3,Tint(Cream,a));
            Stroke(new[] { p+new Vector2(-105,12), p+new Vector2(-67,17),p+new Vector2(-45,9), p+new Vector2(35,12) }, Tint(Earth,a), 3);
        }
    }

    private void Frenzy(float t, float a)
    {
        // Five curved, separately tapered flame tongues, not straight laser beams.
        float progress = Smooth(0, .45f, t);
        for (int i = 0; i < 5; i++)
        {
            Vector2[] path = new Vector2[16];
            for (int j = 0; j < path.Length; j++)
            {
                float u = progress * j / (path.Length - 1);
                path[j] = Source.Lerp(new Vector2((i%2)*18, (i-2)*17), u)
                    + new Vector2(0, MathF.Sin(u*MathF.PI*2.2f+i*.8f)*(26+i*6)*MathF.Sin(u*MathF.PI));
            }
            for (int j = 1; j < path.Length; j++)
            {
                float width = (.5f+11*MathF.Sin(j/(float)path.Length*MathF.PI)) * (1-t*.4f);
                DrawLine(path[j-1], path[j], Tint(Orange,a), width+3, true);
                DrawLine(path[j-1], path[j], Tint(Gold,a), width, true);
            }
            Vector2 tipDirection = (path[^1]-path[^2]).Normalized();
            Triangle(path[^1]+tipDirection*15,path[^2]+tipDirection.Orthogonal()*3,path[^2]-tipDirection.Orthogonal()*3,Tint(Gold,a));
        }
    }

    private void Claw(float t, float a)
    {
        float advance = Smooth(0,.3f,t);
        for (int i = 0; i < 3; i++)
        {
            Vector2 p = new(-36+i*27, -45+advance*40);
            Vector2[] curve = new Vector2[19];
            for(int j=0;j<curve.Length;j++)
            {
                float u=j/18f;
                curve[j]=p+new Vector2(-25+MathF.Sin(u*MathF.PI)*28,-58+115*u);
            }
            Ribbon(curve,12,Tint(Ink,a));
            Ribbon(curve,8,Tint(Violet,a));
            Ribbon(curve,2,Tint(Bone,a));
        }
    }

    private void Helen(float t, float a)
    {
        float push = Smooth(0,.3f,t);
        Vector2 p = new(-28+push*50,-5);
        Triangle(p+new Vector2(-138,-6),p+new Vector2(40,0),p+new Vector2(-110,8),Tint(Ghost,a));
        DrawLine(p+new Vector2(-140,0),p+new Vector2(40,0),Tint(Bone,a),2,true);
        Diamond(p+new Vector2(35,0), 15*(1-t), 25*(1-t),Tint(Bone,a));
    }

    private void Hammer(float t, float a)
    {
        float swing = Smooth(0,.38f,t);
        Vector2 p = new(15-30*swing,-106+135*swing);
        if (t < .6f)
        {
            DrawLine(p+new Vector2(-44,-64),p,Tint(Ghost,a*.75f),8,true);
            // Lobed pumpkin-shaped hammer head; no generic axe blade.
            for (int i = -1; i <= 1; i++)
            {
                DrawCircle(p+new Vector2(i*13,0),20,Tint(Ink,a));
                DrawCircle(p+new Vector2(i*12,0),16,Tint(i==0 ? Earth : Ghost,a));
            }
            DrawLine(p+new Vector2(0,-15),p+new Vector2(0,16),Tint(Bone,a),3,true);
        }
        if (t >= .3f) Burst(new Vector2(-15,36),(t-.3f)/.7f,Earth,a,7);
    }

    private void Skeleton(float t, float a)
    {
        Vector2 p = new(-30+Smooth(0,.3f,t)*53,0);
        // Five jointed fingers fan from a bare skeletal palm, separate from spectral thrust/hammer.
        for (int i=0;i<5;i++)
        {
            Vector2 root=p+new Vector2(-24+i*12,10);
            Vector2 tip=root+new Vector2((i-2)*12,-50+Math.Abs(i-2)*8);
            Stroke(new[] {root,root.Lerp(tip,.55f)+new Vector2(8,-4),tip},Tint(Ink,a),11);
            Stroke(new[] {root,root.Lerp(tip,.55f)+new Vector2(8,-4),tip},Tint(Bone,a),5);
        }
        Stroke(new[] {p+new Vector2(-26,8),p+new Vector2(-17,37),p+new Vector2(18,37),p+new Vector2(27,8)},Tint(Ghost,a),7);
        Ellipse(p,60+40*t,37+20*t,Tint(Ghost,a*.6f),3, -.9f,1.8f);
    }

    private void Heal(float t,float a)
    {
        a *= Smooth(0,.12f,t);
        Ellipse(new Vector2(0,52),45+12*t,12,Tint(Gold,a*.8f),3);
        for (int i=0;i<3;i++)
        {
            float x=(i-1)*27, rise=Smooth(0,.7f,t)*(95+i%2*25);
            Triangle(new Vector2(x-9,49),new Vector2(x,-12-rise),new Vector2(x+9,49),Tint(Gold,a*.24f));
            DrawLine(new Vector2(x,38),new Vector2(x,25-rise),Tint(Gold,a*.7f),3,true);
            Diamond(new Vector2(x,15-rise),4,10,Tint(Cream,a));
        }
        Ellipse(new Vector2(0,30-t*70),48,15,Tint(Gold,a*.7f),2,.15f,2.7f);
    }

    private void Burst(Vector2 center,float t,Color color,float a,int count)
    {
        for(int i=0;i<count;i++)
        {
            Vector2 d=Vector2.FromAngle(-MathF.PI+i*MathF.PI/(count-1));
            Vector2 n=d.Orthogonal(), p=center+d*(12+55*t);
            Triangle(p+d*13*(1-t),p-n*4,p+n*4,Tint(color,a));
        }
    }
    private void Ellipse(Vector2 p,float rx,float ry,Color c,float width,float start=0,float end=MathF.Tau)
    {
        Vector2[] points=new Vector2[33];
        for(int i=0;i<points.Length;i++) { float a=Mathf.Lerp(start,end,i/32f); points[i]=p+new Vector2(MathF.Cos(a)*rx,MathF.Sin(a)*ry); }
        Stroke(points,c,width);
    }
    private void Stroke(Vector2[] points,Color c,float width) { if(c.A>.001f) DrawPolyline(points,c,width,true); }
    private void Ribbon(Vector2[] points,float width,Color color)
    {
        if (color.A<=.001f) return;
        // One closed polygon with shared averaged normals: segment-by-segment
        // quads leave visible pinholes at curved joins in the compatibility renderer.
        Vector2[] normals=new Vector2[points.Length];
        for(int i=1;i<points.Length-1;i++)
            normals[i]=(points[i+1]-points[i-1]).Normalized().Orthogonal()
                * (MathF.Sin(i*MathF.PI/(points.Length-1))*width);
        Vector2[] outline=new Vector2[points.Length*2-2];
        for(int i=0;i<points.Length;i++) outline[i]=points[i]+normals[i];
        for(int i=points.Length-2;i>=1;i--)
        {
            outline[points.Length*2-2-i]=points[i]-normals[i];
        }
        DrawColoredPolygon(outline,color);
    }
    private void Diamond(Vector2 p,float x,float y,Color c)
    {
        if(x>.01f && y>.01f && c.A>.001f) DrawColoredPolygon(new[]{p+new Vector2(-x,0),p+new Vector2(0,-y),p+new Vector2(x,0),p+new Vector2(0,y)},c);
    }
    private void Triangle(Vector2 a,Vector2 b,Vector2 c,Color color)
    {
        if(color.A>.001f && Math.Abs((b-a).Cross(c-a))>.001f) DrawColoredPolygon(new[]{a,b,c},color);
    }
    private static Color Tint(Color c,float a)=>new(c.R,c.G,c.B,Math.Clamp(a,0,1));
    private static float Smooth(float lo,float hi,float t) { t=Math.Clamp((t-lo)/(hi-lo),0,1); return t*t*(3-2*t); }
}
