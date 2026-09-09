#nullable enable
using System;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Deterministic drawing primitives. Each attack owns its silhouette and timing.</summary>
public abstract partial class CombatVfxCanvas : Node2D
{
    protected static float Ease(float a,float b,float t)
    { t=Math.Clamp((t-a)/(b-a),0,1);return t*t*(3-2*t); }
    protected static Color Fade(Color c,float a)=>new(c.R,c.G,c.B,Math.Clamp(a,0,1));
    protected void Poly(Color c,params Vector2[] points)
    {
        if(c.A<=.001f||points.Length<3)return;
        float area=0;for(int i=0;i<points.Length;i++)area+=points[i].Cross(points[(i+1)%points.Length]);
        if(Math.Abs(area)>.01f)DrawColoredPolygon(points,c);
    }
    protected void Shard(Vector2 p,float length,float width,float angle,Color c)
    {
        if(length<=.01f||width<=.01f)return;
        Vector2 d=Vector2.FromAngle(angle),n=d.Orthogonal();
        Poly(c,p-d*length,p-d*length*.2f+n*width,p+d*length,p+d*length*.15f-n*width);
    }
    protected void Band(Vector2 p,Vector2 radii,float start,float end,float width,float angle,Color c,bool taper=true)
    {
        if(c.A<=.001f||width<=.01f||Math.Abs(end-start)<.002f)return;
        const int count=48;
        Vector2 At(float u,bool inner)
        {
            float a=Mathf.Lerp(start,end,u);
            float w=inner?width*(taper?MathF.Pow(Math.Max(0,MathF.Sin(u*MathF.PI)),.65f):1):0;
            return p+new Vector2(MathF.Cos(a)*(radii.X-w),MathF.Sin(a)*(radii.Y-w)).Rotated(angle);
        }
        for(int i=0;i<count;i++)
        {
            float u=i/(float)count,v=(i+1)/(float)count;
            Poly(c,At(u,false),At(v,false),At(v,true));Poly(c,At(u,false),At(v,true),At(u,true));
        }
    }
    protected void Stroke(Vector2[] points,float width,Color c,bool taper=true)
    {
        if(c.A<=.001f||width<=.01f)return;
        if(points.Length<=12)
        {
            DrawPolyline(points,c,width,true);return;
        }
        // Shared ribbon edges avoid round-cap overdraw. Triangles also remain valid
        // while a wide stroke is first being revealed along a very short curve.
        var outline=new Vector2[points.Length*2];
        for(int i=0;i<points.Length;i++)
        {
            Vector2 tangent=points[Math.Min(points.Length-1,i+1)]-points[Math.Max(0,i-1)];
            float u=i/(float)(points.Length-1),w=width*.5f*(taper?.02f+.98f*MathF.Sin(u*MathF.PI):1);
            Vector2 normal=tangent.Normalized().Orthogonal()*w;
            outline[i]=points[i]+normal;outline[outline.Length-1-i]=points[i]-normal;
        }
        for(int i=0;i<points.Length-1;i++)
        {
            int opposite=outline.Length-1-i;
            Poly(c,outline[i],outline[i+1],outline[opposite-1]);
            Poly(c,outline[i],outline[opposite-1],outline[opposite]);
        }
    }
    protected void Cloud(Vector2 center,Vector2 radius,float phase,Color c)
    {
        if(c.A<=.001f)return;
        var points=new Vector2[40];
        for(int i=0;i<points.Length;i++)
        {
            float a=i*MathF.Tau/points.Length,edge=1+.12f*MathF.Sin(a*5+phase)+.055f*MathF.Sin(a*9-phase);
            points[i]=center+new Vector2(MathF.Cos(a)*radius.X,MathF.Sin(a)*radius.Y)*edge;
        }
        Poly(c,points);
    }
    protected void Splinters(float t,Vector2 center,Color c,int count,float reach,float angle=0)
    {
        float expand=Ease(0,.65f,t),fade=1-Ease(.45f,1,t);
        for(int i=0;i<count;i++)
        {
            float a=angle+i*MathF.Tau/count+.16f*MathF.Sin(i*2.7f);
            Vector2 d=Vector2.FromAngle(a);
            Shard(center+d*(45+reach*expand),(20+9*(i%3))*(1-.6f*t),5+3*(i%2),a,Fade(c,c.A*fade));
        }
    }
    protected static Vector2[] Curve(Vector2 from,Vector2 control,Vector2 to,float progress=1)
    {
        var points=new Vector2[29];
        for(int i=0;i<points.Length;i++)
        {float u=i/(float)(points.Length-1)*progress;points[i]=from*(1-u)*(1-u)+control*2*u*(1-u)+to*u*u;}
        return points;
    }
}
