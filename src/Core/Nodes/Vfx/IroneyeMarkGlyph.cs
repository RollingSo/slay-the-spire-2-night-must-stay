#nullable enable
using System;
using Godot;
namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>The marker's arrival collapses to a readable persistent reticle, not a permanent attack flash.</summary>
public partial class IroneyeMarkGlyph : CombatVfxCanvas
{
    private static readonly Color Acid=new("#C8D94A"),Cyan=new("#4FC4C9"),Ink=new("#17272A");
    private Sprite2D? _arrival;
    public override void _Ready()
    {
        _arrival=new Sprite2D{Texture=ParticleVfxMaterials.Texture(5),Material=ParticleVfxMaterials.Material(5,Cyan,.008f)};
        AddChild(_arrival);
    }
    public void DrawMark(float time,float pulse)
    {
        pulse=Math.Clamp(pulse,0,1);
        float settle=Ease(0,1,1-pulse),radius=52+122*(1-settle);
        float idle=.32f+.04f*MathF.Sin(time*2.4f);
        if(_arrival!=null)
        {
            _arrival.Scale=Vector2.One*(radius*2.7f)/_arrival.Texture.GetWidth();
            _arrival.Modulate=new Color(1,1,1,pulse*.5f);
            _arrival.Rotation=time*.7f;
            ((ShaderMaterial)_arrival.Material).SetShaderParameter("phase",time);
        }
        for(int i=0;i<4;i++)
        {
            float angle=-MathF.PI/4+i*MathF.PI/2;
            Vector2 d=Vector2.FromAngle(angle),n=d.Orthogonal(),p=d*radius;
            float length=16+24*pulse,width=7+12*pulse;
            Poly(Fade(Ink,idle+pulse*.6f),p-d*length*1.2f,p+n*width*1.4f,p+d*length*.6f,p-n*width*1.4f);
            Poly(Fade(i%2==0?Acid:Cyan,idle+pulse*.55f),p-d*length,p+n*width,p+d*length*.35f,p-n*width);
        }
        if(pulse>.01f)
        {
            // Open aiming brackets converge; application never fakes a Mark damage fracture.
            Band(Vector2.Zero,new(radius+23,radius*.77f+16),-.65f,.65f,9,0,Fade(Cyan,pulse*.5f));
            Band(Vector2.Zero,new(radius+23,radius*.77f+16),2.49f,3.79f,9,0,Fade(Acid,pulse*.5f));
        }
        Shard(Vector2.Zero,10+6*pulse,3,-.7f,Fade(Acid,idle+pulse*.3f));
    }
}
