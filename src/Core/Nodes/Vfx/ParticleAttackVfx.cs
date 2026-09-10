#nullable enable
using System;
using System.Collections.Generic;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Production textured sprites, flowing/dissolving shaders and native GPU particles.</summary>
public abstract partial class ParticleAttackVfx : Node2D
{
    protected const int Smoke=0, Flame=1, Ice=2, Dust=3, Slash=4, Ring=5, Bolt=6, Impact=7,
        Rapier=8, Hammer=9, Palm=10, Rock=11, Arrow=12, Feather=13, Fracture=14, Light=15;
    public decimal VisualDamage { get; set; } = 8m;
    public Action? OnStart { get; set; }
    public abstract float Duration { get; }
    protected abstract int EffectIndex { get; }
    protected virtual Vector2 Route(float t) => Vector2.Zero;
    protected virtual Vector2 Origin => Vector2.Zero;
    protected virtual float PowerScale => 1f;
    // Scale artwork and local offsets, never the world-space projectile route.
    private float ArtScale => PowerScale * (EffectIndex switch {
        0 => .65f, 1 => .63f, 2 => .62f, 3 or 6 => .55f, 4 or 7 => .52f,
        5 => .60f, 8 => .68f, 9 or 10 => .58f, 11 or 12 or 13 or 14 or 16 => .64f,
        15 or 19 or 20 or 21 => .63f, 17 => .66f, 18 => .60f, _ => 1f });
    private readonly List<(Sprite2D Node, ShaderMaterial Material, Action<Sprite2D,float> Animate, float Start,float End)> _layers=new();
    private readonly List<(GpuParticles2D Node, float Start, bool Follow)> _emitters=new();
    private readonly HashSet<GpuParticles2D> _started=new();
    private float _age, _lastSeek;
    private bool _built, _manual;
    private static readonly Color Silver=new("#BFE9F4"), Gold=new("#ECCA69"), Poison=new("#91D94B"),
        Cyan=new("#73D8E7"), Orange=new("#F29F3F"), Violet=new("#B09AE9"), Earth=new("#C5AC83");
    protected static float Ease(float a,float b,float t) { float u=Math.Clamp((t-a)/(b-a),0,1);return u*u*(3-2*u); }
    public override void _Ready() { Build(); UpdateFrame(0); OnStart?.Invoke(); OnStart=null; }
    public override void _Process(double delta)
    {
        if(_manual)return;
        _age+=(float)delta;
        if(_age>=Duration){QueueFree();return;}
        UpdateFrame(_age/Duration);
    }
    /// <summary>Preview playback scales the actual GPU clock to the requested frame delta.</summary>
    public void Seek(float t)
    {
        Build();_manual=true;t=Math.Clamp(t,0,1);
        if(t<_lastSeek){_started.Clear();foreach(var e in _emitters){e.Node.Restart(true);e.Node.Emitting=false;}}
        float previous=t<_lastSeek?0:_lastSeek;
        UpdateFrame(t);
        foreach(var e in _emitters)
        {
            float seconds=Math.Max(0,t*Duration-e.Start)-Math.Max(0,previous*Duration-e.Start);
            e.Node.SpeedScale=seconds/Math.Max(.0001f,(float)GetProcessDeltaTime());
        }
        _lastSeek=t;
    }
    public void DrawFrame(float t) => Seek(t);
    private void UpdateFrame(float t)
    {
        foreach(var layer in _layers)
        {
            float u=Math.Clamp((t-layer.Start)/(layer.End-layer.Start),0,1);
            float fade=Ease(0,.09f,u)*(1-Ease(.55f,1,u));
            layer.Node.Visible=t>=layer.Start&&t<layer.End;
            layer.Node.Modulate=new Color(1,1,1,fade);
            layer.Material.SetShaderParameter("phase",t*Duration*3);
            layer.Material.SetShaderParameter("dissolve",Ease(.55f,1,u)*.88f);
            layer.Animate(layer.Node,u);
        }
        foreach(var e in _emitters)
        {
            bool active=t*Duration>=e.Start;
            if(active&&_started.Add(e.Node))e.Node.Restart(true);
            if(e.Follow)e.Node.Position=Route(t);
            e.Node.Modulate=new Color(1,1,1,1-Ease(.8f,1,t));
            ((ShaderMaterial)e.Node.Material).SetShaderParameter("phase",t*3);
        }
    }
    private void Layer(int tile,Color color,Vector2 size,Vector2 position,float angle=0,float start=0,float end=1,
        float flow=.01f,Action<Sprite2D,float>? motion=null)
    {
        Texture2D texture=ParticleVfxMaterials.Texture(tile);
        Vector2 scale=size/texture.GetSize()*ArtScale;
        var material=ParticleVfxMaterials.Material(tile,color,flow);
        material.SetShaderParameter("gain",1+AttackVfxSizing.Strength(VisualDamage)*.4f);
        var node=new Sprite2D{Texture=texture,Material=material,Position=position,Scale=scale,Rotation=angle};
        AddChild(node);
        _layers.Add((node,material,(sprite,u)=>{
            sprite.Scale=scale*(.84f+.25f*Ease(0,.7f,u));sprite.Rotation=angle;sprite.Position=position*ArtScale;
            motion?.Invoke(sprite,u);
        },start,end));
    }
    private void Burst(int tile,Color color,int count,float particleSize,float speed,Vector2 position,
        float start=.04f,bool follow=false,float spread=180,Vector3? direction=null)
    {
        var texture=ParticleVfxMaterials.Texture(tile);
        var gradient=new Gradient{Offsets=new[]{0f,.12f,.55f,1f},Colors=new[]{new Color(1,1,1,0),Colors.White,new Color(1,1,1,.7f),new Color(1,1,1,0)}};
        var scaleCurve=new Curve();scaleCurve.AddPoint(new Vector2(0,.4f));scaleCurve.AddPoint(new Vector2(.25f,1));scaleCurve.AddPoint(new Vector2(1,.35f));
        var process=new ParticleProcessMaterial{
            ParticleFlagDisableZ=true,Direction=direction??new Vector3(0,-1,0),Spread=spread,
            EmissionShape=ParticleProcessMaterial.EmissionShapeEnum.Sphere,EmissionSphereRadius=35*ArtScale,
            InitialVelocityMin=speed*.45f*ArtScale,InitialVelocityMax=speed*ArtScale,
            Gravity=new Vector3(0,tile==Smoke?-42:tile==Dust?95:15,0)*ArtScale,
            DampingMin=30,DampingMax=65,AngularVelocityMin=-65,AngularVelocityMax=65,
            ScaleMin=particleSize/texture.GetWidth()*.6f*ArtScale,ScaleMax=particleSize/texture.GetWidth()*ArtScale,
            ScaleCurve=new CurveTexture{Curve=scaleCurve},ColorRamp=new GradientTexture1D{Gradient=gradient}
        };
        var emitter=new GpuParticles2D{
            Texture=texture,Material=ParticleVfxMaterials.Material(tile,color,tile==Smoke?.025f:.005f),ProcessMaterial=process,
            // Sparse secondary accents; damage grows silhouettes, never a cloud of fine lines.
            Amount=Math.Clamp(count/4,2,6),Lifetime=Math.Max(.25f,Duration-start-.08f),OneShot=true,Explosiveness=follow?0f:.94f,
            Randomness=.25f,Emitting=false,UseFixedSeed=true,Seed=(uint)(4400+EffectIndex*17+_emitters.Count),
            FixedFps=60,LocalCoords=false,Position=position*ArtScale,
            VisibilityRect=new Rect2(-1000,-900,2000,1800)
        };
        AddChild(emitter);_emitters.Add((emitter,start,follow));
    }
    private void Hit(Color color,float size=440,float start=.05f)
    {
        Layer(Impact,color,new(size,size*.82f),Vector2.Zero,start:start,end:.75f);
        Burst(Impact,color,14,62,220,Vector2.Zero,start*Duration);
    }
    private void Sweep(Color color,float size=560,float angle=0,float start=0)
    {
        Layer(Slash,color,new(size,size*.85f),Vector2.Zero,angle,start,1,.012f,
            (s,u)=>s.Rotation=angle-.35f+.62f*Ease(0,.55f,u));
    }
    private void Build()
    {
        if(_built)return;_built=true;
        float power=AttackVfxSizing.Strength(VisualDamage);
        switch(EffectIndex)
        {
            case 0: // Halberd: broad steel leading edge, weighty secondary wake.
                Sweep(Silver,590,-.4f);Hit(Silver,390,.07f);Burst(Dust,Silver,7,130,110,new(0,70));break;
            case 1: // Guardian wing wind remains horizontal, never a tornado funnel.
                for(int i=0;i<2;i++){int row=i;Layer(Slash,new Color(.62f,.82f,.88f,.72f),new(680-i*25,320),new(0,-80+i*155),i%2==0?-.1f:3.05f,
                    flow:.035f,motion:(s,u)=>s.Position+=new Vector2(MathF.Sin(u*4+row)*50,0)*ArtScale);}
                Burst(Feather,new Color(.66f,.65f,.55f),10,85,160,Vector2.Zero);break;
            case 2:
                Layer(Ring,Silver,new(230,290),new(-90,0),start:0,end:.24f);
                Sweep(Silver,600,.5f,.1f);Hit(Gold,430+power*35,.13f);Burst(Dust,Earth,10,140,150,new(0,90),.13f);break;
            case 3:case 6:
                Color knife=EffectIndex==6?Poison:Silver;Sweep(knife,565,-.3f);Hit(knife,350,.08f);
                Burst(EffectIndex==6?Smoke:Impact,knife,EffectIndex==6?10:16,EffectIndex==6?130:45,190,Vector2.Zero);break;
            case 4:case 7:
                Color arrow=EffectIndex==7?Poison:Silver;
                float arrival=.16f/Duration;
                Layer(Arrow,arrow,new(300,210),Origin,start:0,end:arrival,flow:0,motion:(s,u)=>{
                    Vector2 forward=Origin.LengthSquared()>1?-Origin.Normalized():Vector2.Right;
                    s.Rotation=forward.Angle();
                    s.Position=Route(u*arrival)-forward*(s.Texture.GetWidth()*s.Scale.X*.43f);
                });
                Hit(arrow,540,arrival);
                Burst(EffectIndex==7?Smoke:Impact,arrow,14,EffectIndex==7?130:58,180,Vector2.Zero,.16f,spread:55,direction:new Vector3(1,0,0));break;
            case 5:
                Layer(Fracture,Cyan,new(550,550),Vector2.Zero,start:.05f,flow:0);
                Burst(Ice,Poison,12,60,230,Vector2.Zero,.1f);break;
            case 8:
                Layer(Smoke,new Color(.35f,.65f,.15f,.85f),new(600,570),Vector2.Zero,flow:.06f);
                Layer(Smoke,Poison,new(450,430),new(-30,-10),.8f,0,.88f,.055f);
                Burst(Smoke,Poison,18,170,190,Vector2.Zero);break;
            case 9:case 10:
                Layer(Ring,Gold,new(560+power*28,440+power*25),Vector2.Zero,flow:.008f,motion:(s,u)=>{
                    s.Position=Route(u);s.Rotation=u*1.2f;if(EffectIndex==10)s.Scale*=1-.68f*Ease(.73f,1,u);
                });
                Burst(Impact,Gold,20,28+power*9,80,Origin,0,true);break;
            case 11:case 12:case 13:
                Color lightning=EffectIndex==11?new Color("#F36775"):EffectIndex==12?Gold:Cyan;
                int branches=EffectIndex==11?2:1;
                for(int i=0;i<branches;i++){float x=(i-(branches-1)*.5f)*90;Layer(Bolt,lightning,new(EffectIndex==12?270:320,550),new(x,-105),x*.0015f,0,.83f,.012f);}
                Hit(lightning,560,.035f);
                if(EffectIndex==13)Layer(Ice,Cyan,new(390,370),new(0,45),start:.12f);
                Burst(EffectIndex==13?Ice:Impact,lightning,19,75,230,new(0,70));break;
            case 14:
                Layer(Rock,Earth,new(340,340),Origin,start:0,end:.49f,flow:0,motion:(s,u)=>{s.Position=Route(u);s.Rotation=u*1.5f;});
                Layer(Dust,Earth,new(650,400),new(0,70),start:.4f,flow:.05f);
                Hit(Earth,420,.42f);Burst(Rock,Earth,14,110,260,Vector2.Zero,Duration*.4f);break;
            case 15:
                for(int i=0;i<3;i++)Layer(Slash,Earth,new(220,580),new(-120+i*120,0),-.3f, i*.025f,1,.013f);
                Layer(Dust,Earth,new(600,260),new(0,130),start:.12f,flow:.05f);Burst(Rock,Earth,12,65,175,new(0,95));break;
            case 16:
                for(int i=0;i<3;i++){int ray=i;Layer(Flame,i%2==0?Gold:Orange,new(260,610),new(-90+i*80,-65+i*60),1.05f+i*.25f,i*.025f,1,.045f,
                    (s,u)=>s.Position+=new Vector2(Ease(0,.5f,u)*120,MathF.Sin(u*5+ray)*17)*ArtScale);}
                Burst(Flame,Orange,20,110,200,Vector2.Zero,spread:45,direction:new Vector3(1,0,0));break;
            case 17:
                for(int i=0;i<3;i++)Layer(Slash,Violet,new(220,500),new(-105+i*105,0),-.15f,i*.04f,1,.018f);
                Burst(Smoke,Violet,9,135,120,Vector2.Zero);break;
            case 18:
                Layer(Rapier,Silver,new(580,400),new(-60,0),start:0,end:.65f,flow:0,motion:(s,u)=>s.Position+=new Vector2(120*Ease(0,.28f,u),0)*ArtScale);
                Hit(Silver,460,.16f);break;
            case 19:
                Layer(Hammer,new Color(.82f,.86f,.81f),new(510,510),new(0,-50),start:0,end:.74f,flow:0,
                    motion:(s,u)=>{s.Rotation=-.75f+1.1f*Ease(0,.5f,u);s.Position+=new Vector2(0,110*Ease(0,.5f,u))*ArtScale;});
                Layer(Dust,Earth,new(620,350),new(0,90),start:.35f,flow:.05f);Hit(Gold,400,.36f);
                Burst(Rock,Earth,15,95,235,new(0,80),Duration*.35f);break;
            case 20:
                Layer(Smoke,new Color(.35f,.62f,.7f,.5f),new(510,490),new(0,0),flow:.06f);
                Layer(Palm,Silver,new(490,540),Vector2.Zero,flow:0,motion:(s,u)=>{s.Rotation=-.28f+.5f*Ease(0,.5f,u);s.Scale*=.8f+.25f*Ease(0,.4f,u);});
                Burst(Smoke,Cyan,11,125,140,Vector2.Zero,.1f);break;
            case 21:
                Layer(Light,new Color(1,.84f,.4f,.65f),new(390,500),Vector2.Zero,flow:.008f,
                    motion:(s,u)=>s.Position-=new Vector2(0,80*Ease(0,.85f,u))*ArtScale);
                Layer(Ring,Gold,new(540,220),new(0,105),start:.02f);
                Burst(Impact,Gold,24,32,130,new(0,100),spread:25,direction:new Vector3(0,-1,0));break;
            default: throw new InvalidOperationException("Unmapped VFX type " + EffectIndex);
        }
    }
}
