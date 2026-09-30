using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Viewport-space time ribbons; no combat state, RNG, damage or UI mutation.</summary>
public partial class DuchessReverseTimeVfx : CombatVfxCanvas
{
    public const float RewindCue = 1.625f;
    public const float Lifetime = 2.4f;
    private float _age;
    private bool _manual;
    public bool SoundEnabled { get; set; }
    private bool _rewindSound;
    public override void _Ready()
    {
        if (SoundEnabled && !_manual) DuchessAudio.Play("plasma_orb_channel.mp3", .55f);
    }
    public bool CleanupLayer { get; set; }
    public void Seek(float progress) { _manual=true; _age=Math.Clamp(progress,0,1)*Lifetime; QueueRedraw(); }
    public override void _Process(double delta)
    {
        if(_manual)return;
        _age+=(float)delta;
        if (SoundEnabled && !_rewindSound && _age >= RewindCue)
        {
            _rewindSound = true;
            DuchessAudio.Play("dark_orb_evoke.mp3", .65f);
        }
        if(_age>=Lifetime) { if(CleanupLayer)GetParent().QueueFree();else QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        Vector2 size=GetViewportRect().Size;
        Vector2 center=new(size.X*.5f,size.Y*.46f);
        float unit=Math.Min(size.X/1440f,size.Y/960f);
        float t=Math.Clamp(_age/Lifetime,0,1);
        float fade=Ease(0,.1f,t)*(1-Ease(.72f,1,t));
        Color cyan=Fade(new Color("59D9FF"),fade*.8f), core=Fade(new Color("D3F9FF"),fade*.85f);
        Color blue=Fade(new Color("2465B4"),fade*.4f);
        DrawRect(new Rect2(Vector2.Zero,size),new Color(.015f,.045f,.12f,fade*.25f));
        // First swell outward over the battlefield; after the cue every trail runs backward.
        float swell=Ease(.04f,.5f,t);
        float returnPhase=Ease(RewindCue/Lifetime,.94f,t);
        float rotation=t<RewindCue/Lifetime ? -t*5.3f : -RewindCue/Lifetime*5.3f+returnPhase*3.6f;
        float radius=(70+swell*size.Length()*.52f)*(1-returnPhase*.83f);
        for(int arm=0;arm<5;arm++)
        {
            var ribbon=new Vector2[65];
            for(int i=0;i<ribbon.Length;i++)
            {
                float u=i/(float)(ribbon.Length-1);
                float angle=rotation+arm*MathF.Tau/5+u*4.8f;
                float reach=radius*(.14f+.86f*u);
                ribbon[i]=center+new Vector2(MathF.Cos(angle)*reach,MathF.Sin(angle)*reach*.67f);
            }
            Stroke(ribbon,(24+arm%2*8)*unit,blue);
            Stroke(ribbon,(13+arm%2*5)*unit,cyan);
            Stroke(ribbon,3*unit,core);
        }
        // Backward-running clock marks unify the temporal ribbons with the Duchess.
        float dialRadius=(105+65*swell)*(1-returnPhase*.4f)*unit;
        DrawArc(center,dialRadius,0,MathF.Tau,96,Fade(core,fade*.4f),2*unit,true);
        for(int i=0;i<12;i++)
        {
            Vector2 direction=Vector2.FromAngle(i*MathF.Tau/12-t*4);
            DrawLine(center+direction*(dialRadius-15*unit),center+direction*dialRadius,core,3*unit,true);
        }
        DrawLine(center,center+Vector2.FromAngle(-t*18)*dialRadius*.75f,core,4*unit,true);
        DrawLine(center,center+Vector2.FromAngle(-t*5+1)*dialRadius*.45f,core,5*unit,true);
        // Broad echoes sweep enemy and ally positions without obscuring their silhouettes.
        for(int i=0;i<18;i++)
        {
            float phase=(t*1.7f+i*.061f)%1;
            float x=size.X*(1-phase);
            float y=size.Y*(.17f+(i%6)*.12f);
            Vector2 p=new(x,y);
            Shard(p,(35+i%3*17)*unit,3*unit,-.25f,Fade(cyan,fade*.45f));
        }
        if(t>RewindCue/Lifetime)
        {
            // Trails converge on the lower UI while native card-pile movement and
            // SetEnergy animate the real resources; these are not fake card copies.
            Vector2 energy=new(size.X*.13f,size.Y*.86f);
            Vector2 drawPile=new(size.X*.055f,size.Y*.9f);
            Vector2 discard=new(size.X*.94f,size.Y*.9f);
            float u=Ease(RewindCue/Lifetime,.94f,t);
            if(u>.025f)
            {
                Stroke(Curve(discard,new Vector2(size.X*.5f,size.Y*.61f),drawPile,u),8*unit,cyan);
                Stroke(Curve(center,new Vector2(size.X*.25f,size.Y*.35f),energy,u),5*unit,core);
            }
            DrawArc(energy,22*unit,0,MathF.Tau,48,Fade(core,fade*.8f),3*unit,true);
        }
    }
}
