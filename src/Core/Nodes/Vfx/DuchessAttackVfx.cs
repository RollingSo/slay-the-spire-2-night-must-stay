using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Presentation only: target-local impacts and source-to-target flight, no combat RNG.</summary>
public partial class DuchessAttackVfx : CombatVfxCanvas
{
    public enum Kind { Glintblade, Slicer, GreatCaria, Greatsword, Piercer, Clock,
        Greatbow, Mastery, DeathBlade, GoldenBlade, Miquella, Sacred,
        LorettaSlash, SilverStorm, OpeningMoment }
    public Kind AttackKind { get; set; }
    public Vector2 Source { get; set; } = new(-300,0);
    private float _age;
    private bool _manual;
    public bool SoundEnabled { get; set; }
    private DuchessAudio.Cue[] _soundCues = Array.Empty<DuchessAudio.Cue>();
    private int _nextSound;
    private Sprite2D _impact;
    private Sprite2D _wake;
    public override void _Ready()
    {
        _soundCues = DuchessAudio.AttackCues(AttackKind);
        UpdateSound();
        Color tint = AttackKind switch {
            Kind.DeathBlade => new Color("E34A67"),
            Kind.GoldenBlade or Kind.Sacred => new Color("F4CB67"),
            Kind.Miquella => new Color("FFF1B5"),
            Kind.SilverStorm => new Color("DAE5EF"),
            Kind.OpeningMoment => new Color("FFB65C"),
            _ => new Color("48B9ED") };
        _impact = new Sprite2D { Texture=ParticleVfxMaterials.Texture(7),
            Material=ParticleVfxMaterials.Material(7,tint),
            Scale=Vector2.One*(220f/ParticleVfxMaterials.Texture(7).GetWidth()), ZIndex=-1 };
        _wake = new Sprite2D { Texture=ParticleVfxMaterials.Texture(4),
            Material=ParticleVfxMaterials.Material(4,tint),
            Scale=new Vector2(280f,160f)/ParticleVfxMaterials.Texture(4).GetWidth(), ZIndex=-1 };
        AddChild(_impact); AddChild(_wake); UpdateLayers();
    }
    private void UpdateLayers()
    {
        if (_impact == null) return;
        float t=Math.Clamp(_age/Duration,0,1);
        float start=AttackKind switch {
            Kind.Clock => .4f, Kind.LorettaSlash => .42f,
            Kind.SilverStorm => .38f, _ => .3f };
        float fade=Ease(start,start+.05f,t)*(1-Ease(.55f,1,t));
        _impact.Modulate=new Color(1,1,1,fade*.8f);
        _impact.Visible=AttackKind != Kind.Clock;
        var material=(ShaderMaterial)_impact.Material;
        material.SetShaderParameter("phase",t*3);
        material.SetShaderParameter("dissolve",Ease(.5f,1,t));
        _wake.Visible=AttackKind is Kind.Slicer or Kind.GreatCaria or Kind.Greatsword or Kind.Piercer;
        _wake.Rotation=-.9f+t*1.8f;
        _wake.Modulate=new Color(1,1,1,Ease(.05f,.2f,t)*(1-Ease(.45f,.8f,t))*.7f);
        ((ShaderMaterial)_wake.Material).SetShaderParameter("phase",t*3);
    }
    public float Duration => AttackKind switch {
        Kind.Clock => .85f, Kind.LorettaSlash => .9f,
        Kind.SilverStorm => .8f, Kind.OpeningMoment => 1f, _ => .72f };
    public void Seek(float progress) { _manual = true; _age = Math.Clamp(progress,0,1)*Duration; UpdateLayers(); QueueRedraw(); }
    public override void _Process(double delta)
    {
        if (_manual) return;
        _age += (float)delta;
        UpdateSound();
        if (_age >= Duration) { QueueFree(); return; }
        UpdateLayers();
        QueueRedraw();
    }
    private void UpdateSound()
    {
        if (!SoundEnabled || _manual) return;
        while (_nextSound < _soundCues.Length
            && _age >= _soundCues[_nextSound].At * Duration)
        {
            var cue = _soundCues[_nextSound++];
            DuchessAudio.Play(cue.File, cue.Volume);
        }
    }
    public override void _Draw()
    {
        float t = Math.Clamp(_age/Duration,0,1);
        float alpha = Ease(0,.06f,t)*(1-Ease(.62f,1,t));
        Color blue = Fade(new Color("48B9ED"),alpha), white = Fade(new Color("D4F7FF"),alpha);
        Color gold = Fade(new Color("F4CB67"),alpha);
        if (AttackKind == Kind.Clock) { Clock(t,Fade(new Color("FFFFFF"),alpha)); return; }
        if (AttackKind == Kind.LorettaSlash) { LorettaSlash(t,blue,white); return; }
        if (AttackKind == Kind.SilverStorm) { SilverStorm(t,alpha); return; }
        if (AttackKind == Kind.OpeningMoment) { OpeningMoment(t,alpha); return; }
        if (AttackKind is Kind.Slicer or Kind.GreatCaria or Kind.Greatsword or Kind.Piercer)
        {
            float angle = AttackKind switch {
                Kind.GreatCaria => Mathf.Lerp(-2.2f,.9f,Ease(.06f,.5f,t)),
                Kind.Greatsword => Mathf.Lerp(-1.9f,1.6f,Ease(.02f,.5f,t)),
                Kind.Piercer => 0f, _ => Mathf.Lerp(-1.4f,.6f,Ease(0,.32f,t)) };
            float length = AttackKind == Kind.Slicer ? 145 : AttackKind == Kind.GreatCaria ? 270 : 220;
            Vector2 pivot = AttackKind == Kind.Piercer ? new Vector2(-240+230*Ease(0,.38f,t),0) : new Vector2(-110,65);
            if (AttackKind != Kind.Piercer)
                Band(pivot,new Vector2(length,length*.65f),angle-.9f,angle,32,0,Fade(blue,alpha*.5f));
            Sword(pivot,length,AttackKind == Kind.GreatCaria ? 25 : 13,angle,blue,white);
            Impact(t,.3f,blue);
            return;
        }
        float flight = Ease(.04f,.36f,t);
        Vector2 tip = Source.Lerp(Vector2.Zero,flight);
        float direction = (-Source).Angle();
        bool bow = AttackKind is Kind.Greatbow or Kind.Mastery;
        if (bow)
        {
            Color bowColor = blue;
            MagicGreatbow(Source,direction,t,bowColor,white);
            int count = AttackKind == Kind.Mastery ? 4 : 1;
            for (int i=0;i<count;i++)
            {
                Vector2 offset = new Vector2(0,(i-(count-1)*.5f)*34*(1-flight)).Rotated(direction);
                Vector2 end = tip+offset;
                DrawLine(end-Vector2.FromAngle(direction)*150,end,bowColor,10,true);
                DrawLine(end-Vector2.FromAngle(direction)*150,end,white,3,true);
                Shard(end,38,13,direction,bowColor);
                Shard(end,31,5,direction,white);
                DrawLine(Source+offset,end,Fade(bowColor,alpha*.22f),2,true);
            }
            Impact(t,.36f,bowColor); return;
        }
        if (AttackKind == Kind.Glintblade)
        {
            Sword(tip-Vector2.FromAngle(direction)*115,115,10,direction,blue,white);
            DrawLine(Source,tip,Fade(blue,alpha*.25f),4,true);
            Impact(t,.36f,blue); return;
        }
        Color color = AttackKind == Kind.DeathBlade ? Fade(new Color("E34A67"),alpha)
            : AttackKind == Kind.Miquella ? Fade(new Color("FFF1B5"),alpha) : gold;
        if (AttackKind is Kind.DeathBlade or Kind.GoldenBlade)
        {
            Color core = AttackKind == Kind.DeathBlade ? Fade(new Color("271426"),alpha) : white;
            Band(tip,new Vector2(65,95),-1.3f,1.3f,20,direction,color);
            Band(tip,new Vector2(52,82),-1.2f,1.2f,10,direction,core);
        }
        else
        {
            float radius = AttackKind == Kind.Miquella ? 48 : 65;
            DrawArc(tip,radius,0,MathF.Tau,64,color,6,true);
            if (AttackKind == Kind.Miquella)
            {
                DrawArc(tip,radius-12,0,MathF.Tau,64,color,2,true);
                for(int i=0;i<8;i++) Shard(tip+Vector2.FromAngle(i*MathF.Tau/8)*radius,15,4,i*MathF.Tau/8+t*5,color);
            }
            else
                DrawArc(tip,radius*.55f,0,MathF.Tau,48,color,3,true);
        }
        Impact(t,.36f,color);
    }
    private void LorettaSlash(float t,Color blue,Color white)
    {
        Vector2 pivot=new(-75,45);
        float sweep=Ease(.06f,.42f,t),angle=Mathf.Lerp(-2.3f,1.1f,sweep);
        // Long shaft and curved sickle blade, distinct from the Carian swords.
        Vector2 d=Vector2.FromAngle(angle),tip=pivot+d*190;
        DrawLine(pivot-d*95,tip,Fade(blue,blue.A*.5f),9,true);
        DrawLine(pivot-d*95,tip,white,3,true);
        Band(tip,new Vector2(72,48),-1.8f,.9f,18,angle,blue);
        Band(tip,new Vector2(70,46),-1.8f,.9f,5,angle,white);
        if(sweep>0)
        {
            Band(pivot,new Vector2(245,185),-2.3f,angle,45,-.15f,Fade(blue,blue.A*.35f));
            Band(pivot,new Vector2(226,170),-2.3f,angle,25,-.15f,blue);
            Band(pivot,new Vector2(222,167),-2.3f,angle,7,-.15f,white);
        }
        Impact(t,.42f,blue);
    }
    private void SilverStorm(float t,float alpha)
    {
        Color silver=Fade(new Color("DAE5EF"),alpha),white=Fade(new Color("FFFFFF"),alpha);
        // One storm per damage event: card execution retains its own 2/3 hits.
        for(int i=0;i<3;i++)
        {
            float u=Ease(.03f+i*.05f,.38f+i*.05f,t);
            if(u<=0)continue;
            Vector2 center=new(0,-55+i*55);
            float start=-2.8f+i*.65f,end=start+u*4.9f;
            Band(center,new Vector2(205-i*15,58+i*6),start,end,17,.15f*(i-1),silver);
            Band(center,new Vector2(204-i*15,57+i*6),start,end,4,.15f*(i-1),white);
            Vector2 tip=center+new Vector2(MathF.Cos(end)*(205-i*15),MathF.Sin(end)*(58+i*6)).Rotated(.15f*(i-1));
            Shard(tip,24,6,end+MathF.PI*.5f,white);
        }
        Impact(t,.38f,silver);
    }
    private void OpeningMoment(float t,float alpha)
    {
        Color gold=Fade(new Color("FFB65C"),alpha),white=Fade(new Color("FFF3D8"),alpha);
        float burst=Ease(.28f,.62f,t),radius=95+burst*105;
        Color face=Fade(gold,gold.A*(1-Ease(.32f,.7f,t)));
        DrawArc(Vector2.Zero,95,0,MathF.Tau,64,face,3,true);
        for(int i=0;i<12;i++)
        {
            float a=i*MathF.Tau/12-MathF.PI*.5f;
            DrawLine(Vector2.FromAngle(a)*80,Vector2.FromAngle(a)*91,face,3,true);
        }
        float hand=Mathf.Lerp(-MathF.PI*1.5f,-MathF.PI*.5f,Ease(0,.28f,t));
        DrawLine(Vector2.Zero,Vector2.FromAngle(hand)*77,face,5,true);
        DrawCircle(Vector2.Zero,5,face);
        if(t>=.28f)
        {
            Band(Vector2.Zero,new Vector2(radius*1.3f,radius*.42f),-MathF.PI,MathF.PI,14,0,white,false);
            Splinters(burst,Vector2.Zero,gold,12,145);
        }
        Impact(t,.3f,gold);
    }
    private void Sword(Vector2 hilt,float length,float width,float angle,Color edge,Color core)
    {
        Vector2 d=Vector2.FromAngle(angle),n=d.Orthogonal();
        Poly(edge,hilt-n*width,hilt+d*(length*.8f)-n*width,hilt+d*length,hilt+d*(length*.8f)+n*width,hilt+n*width);
        Poly(core,hilt,hilt+d*(length*.8f)-n*(width*.3f),hilt+d*length,hilt+d*(length*.8f)+n*(width*.3f));
        DrawLine(hilt-n*width*2,hilt+n*width*2,edge,5,true);
        DrawLine(hilt-d*28,hilt,core,5,true);
    }
    private void Impact(float t,float start,Color color)
    {
        if(t<start)return;
        float u=(t-start)/(1-start);
        Splinters(u,Vector2.Zero,color,12,110);
        DrawArc(Vector2.Zero,12+u*80,0,MathF.Tau,48,Fade(color,color.A*(1-u)),3,true);
    }
    private void MagicGreatbow(Vector2 origin,float angle,float t,Color blue,Color white)
    {
        Vector2 At(Vector2 p)=>origin+p.Rotated(angle);
        float fade=1-Ease(.38f,.7f,t);
        blue=Fade(blue,blue.A*fade);white=Fade(white,white.A*fade);
        // Same broad recurved, pointed blue-white bow for both Loretta spells.
        for(int side=-1;side<=1;side+=2)
        {
            Vector2[] limb=Curve(At(Vector2.Zero),At(new Vector2(66,side*76)),At(new Vector2(-12,side*148)));
            Stroke(limb,22,Fade(blue,blue.A*.28f),false);
            Stroke(limb,13,blue,false);
            Stroke(limb,4,white,false);
            Poly(blue,At(new Vector2(-12,side*148)),At(new Vector2(-55,side*172)),At(new Vector2(-33,side*133)),At(new Vector2(0,side*123)));
            Poly(white,At(new Vector2(-51,side*168)),At(new Vector2(-25,side*142)),At(new Vector2(-9,side*129)));
            Vector2[] engraving=Curve(At(new Vector2(12,side*30)),At(new Vector2(38,side*80)),At(new Vector2(-20,side*130)));
            Stroke(engraving,2,white);
            DrawLine(At(new Vector2(-55,side*172)),At(new Vector2(-50*(1-Ease(.08f,.3f,t)),0)),white,2,true);
        }
        Shard(origin,15,8,angle,white);
    }
    private void Clock(float t,Color white)
    {
        const float breakAt=.48f;
        Color ghost=Fade(white,white.A*.58f);
        // Tilted phantom pocket-watch, open lid and crown, matching Reenactment.
        Vector2 Oval(float a,float radius=1)=>new Vector2(MathF.Cos(a)*96,MathF.Sin(a)*72)*radius;
        if(t<breakAt)
        {
            for(int echo=2;echo>=0;echo--)
            {
                Vector2 offset=new(-echo*16,-echo*10);
                Color ink=Fade(white,white.A*(echo==0?.9f:.13f));
                Vector2[] outline=new Vector2[65];
                for(int i=0;i<outline.Length;i++)outline[i]=Oval(i*MathF.Tau/64)+offset;
                Stroke(outline,echo==0?4:2,ink,false);
            }
            Vector2[] inner=new Vector2[65];
            for(int i=0;i<inner.Length;i++)inner[i]=Oval(i*MathF.Tau/64,.83f);
            Stroke(inner,1.5f,ghost,false);
            for(int i=0;i<60;i++)
            {
                float a=i*MathF.Tau/60;
                DrawLine(Oval(a,i%5==0?.74f:.8f),Oval(a,.88f),i%5==0?white:ghost,i%5==0?3:1,true);
            }
            float rotation=t*12;
            Vector2 minute=Oval(rotation,.7f),hour=Oval(rotation*.3f+1,.46f);
            Shard(minute*.48f,minute.Length()*.5f,4,minute.Angle(),white);
            Shard(hour*.48f,hour.Length()*.5f,5,hour.Angle(),white);
            DrawCircle(Vector2.Zero,7,white);
            DrawArc(new Vector2(0,-88),13,0,MathF.Tau,24,white,3,true);
            DrawLine(new Vector2(-8,-73),new Vector2(8,-73),white,5,true);
            // Open watch cover on the left; no solid opaque clock face.
            Vector2[] lid=new Vector2[49];
            for(int i=0;i<lid.Length;i++){float a=i*MathF.Tau/48;lid[i]=new Vector2(-125+MathF.Cos(a)*35,-28+MathF.Sin(a)*78).Rotated(-.2f);}
            Stroke(lid,3,ghost,false);
            for(int i=0;i<4;i++)DrawArc(new Vector2(18+i*19,-95-i*3),10,0,MathF.Tau,20,ghost,2,true);
        }
        else
        {
            float u=(t-breakAt)/(1-breakAt);
            Color fragment=Fade(white,white.A*(1-u));
            for(int i=0;i<12;i++)
            {
                float angle=i*MathF.Tau/12;
                Vector2 drift=Vector2.FromAngle(angle)*(u*135);
                Vector2[] segment=new Vector2[9];
                for(int j=0;j<9;j++)segment[j]=Oval(angle+j*.045f).Rotated(u*(i%2==0?1:-1)) + drift;
                Stroke(segment,3,fragment,false);
                Shard(Oval(angle)*(.7f+u),18*(1-u*.6f),5,angle+u*3,fragment);
            }
            Splinters(u,Vector2.Zero,fragment,10,110);
        }
    }
}
