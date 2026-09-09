using System;
using Godot;
using NightMustStay.Core.Nodes.Vfx;
using K = NightMustStay.Core.Nodes.Vfx.RevenantAttackVfx.Kind;

public partial class Preview : Node2D
{
    private int _frames;
    private float _time;
    private bool _verify, _capture;
    private readonly string[] _labels = { "HALO / OUTBOUND", "HALO / NEXT-TURN RETURN", "LIGHTNING / RED", "LIGHTNING / YELLOW",
        "LIGHTNING / BLUE", "BEAST / THROWN ROCK", "BEAST / GROUND CLAWS", "FRENZY / CURVED FLAMES",
        "CURSED CLAW", "HELEN / RAPIER THRUST", "FREDERICK / PUMPKIN HAMMER", "SEBASTIAN / SKELETAL PALM", "HEAL / GOLD ON RECIPIENT" };

    public override void _Ready()
    {
        ParticleVfxMaterials.AssetRoot=ProjectSettings.GlobalizePath("res://../../images/vfx/particle_remake/");
        _verify = Array.Exists(OS.GetCmdlineUserArgs(), x => x == "--verify");
        _capture = Array.Exists(OS.GetCmdlineUserArgs(), x => x == "--capture");
        for (int i = 0; i < 13; i++)
            AddChild(new SnapshotVfx { AttackKind = (K)i, FrameTime = i == 5 ? .25f : .36f,
                Position = new Vector2((i==1?100:250)+i%4*360,240+i/4*255), Source = new Vector2(i==1 ? 280 : -280,0), Scale=Vector2.One*.64f });
        if (_verify)
        {
            foreach (K kind in new[] { K.HaloOut, K.HaloReturn, K.BeastRock })
            foreach (Vector2 origin in new[] { new Vector2(-420,70),new Vector2(420,-70),Vector2.Zero })
            {
                var effect = new RevenantAttackVfx { AttackKind=kind,Source=origin };
                if (effect.FlightPoint(0).DistanceTo(origin)>.001f || effect.FlightPoint(1).LengthSquared()>.001f)
                    throw new Exception("Projectile endpoint invariant failed: "+kind);
                effect.Free();
            }
            for (int i=0;i<65;i++) AddChild(new RevenantAttackVfx { AttackKind=(K)(i%13),Position=new Vector2(-1000,-1000) });
            GD.Print("GEOMETRY: 3 projectiles x left/right/zero-length paths have exact endpoints.");
        }
    }

    public override async void _Process(double delta)
    {
        _frames++; _time += (float)delta;
        if (!_capture)
            foreach (Node child in GetChildren())
                if (child is SnapshotVfx sample)
                {
                    sample.FrameTime = _verify ? Math.Clamp((_frames-1)/120f,0,1) : (_time%1.4f)/sample.Duration;
                    sample.QueueRedraw();
                }
        if (_verify && _frames==180)
        {
            int live=0;
            foreach(Node child in GetChildren()) if(child is RevenantAttackVfx && child is not SnapshotVfx) live++;
            GD.Print($"VFX_LIFECYCLE: remaining={live}; 13 kinds sampled at 121 lifetime positions; 65 spawned nodes.");
            GetTree().Quit(live==0?0:1);
        }
        if(_capture && _frames==8)
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            string path=ProjectSettings.GlobalizePath("res://../../design/特效预览/revenant_attack_vfx_contact.png");
            DirAccess.MakeDirRecursiveAbsolute(System.IO.Path.GetDirectoryName(path));
            Error result=GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"VFX_CONTACT_CAPTURE: {result} {path}");
            GetTree().Quit(result==Error.Ok?0:1);
        }
    }

    public override void _Draw()
    {
        var font=ThemeDB.FallbackFont;
        DrawString(font,new Vector2(24,35),"REVENANT / PRAYERS & FAMILY ATTACKS",fontSize:25,modulate:new Color("#F5DDAC"));
        DrawString(font,new Vector2(24,62),"Actual production drawing | target silhouettes for placement | isolated visual preview (silent)",fontSize:16,modulate:new Color("#AAA1B5"));
        for(int i=0;i<13;i++)
        {
            float x=i%4*360,y=85+i/4*255;
            DrawRect(new Rect2(x+10,y,340,245),new Color(i%2==0?"#272535":"#383644"));
            DrawString(font,new Vector2(x+20,y+26),_labels[i],fontSize:16,modulate:new Color("#F5DDAC"));
            float targetX=x+(i==1?100:250);
            DrawCircle(new Vector2(targetX,y+150),24,new Color("#555363"));
            DrawRect(new Rect2(targetX-22,y+167,44,25),new Color("#555363"));
        }
        DrawString(font,new Vector2(390,930),"Halo return is triggered by real next-turn hand recovery.",fontSize:22,modulate:new Color("#F5DDAC"));
        DrawString(font,new Vector2(390,966),"Healing is golden and follows actual HP restored, not block.",fontSize:22,modulate:new Color("#F5DDAC"));
        DrawString(font,new Vector2(390,1002),"Gameplay damage, RNG, charge and resonance are unchanged.",fontSize:22,modulate:new Color("#AAA1B5"));
    }
}
