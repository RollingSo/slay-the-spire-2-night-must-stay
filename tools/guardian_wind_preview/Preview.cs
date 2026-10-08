using System;
using Godot;
using NightMustStay.Core.Nodes.Vfx;
public partial class Preview : Node2D
{
    private double _age;
    private int _capture;
    private bool _capturing;
    private bool _revised, _impactStarted;
    public override void _Ready()
    {
        int i=0;
        _revised=Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--revised");
        if (_revised)
        {
            AddChild(new Label { Text="StormAssault: downward", Position=new Vector2(15,20) });
            AddChild(new Label { Text="PhantomCoStrike: same direction", Position=new Vector2(410,20) });
            AddChild(new Label { Text="Heavenfall: custom layers only", Position=new Vector2(810,20) });
            AddChild(new GuardianStormAttackVfx { AttackKind=GuardianStormAttackVfx.Kind.StormAssault, Position=new Vector2(200,290) });
            AddChild(new GuardianSpecialAttackVfx { AttackKind=GuardianSpecialAttackVfx.Kind.PhantomCoStrike, Position=new Vector2(580,290), Scale=Vector2.One*.8f });
            AddChild(new GuardianHeavenfallPreludeVfx { Position=new Vector2(1000,300), Scale=Vector2.One*.55f });
            return;
        }
        if(Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--storm"))
        {
            foreach(var kind in Enum.GetValues<GuardianStormAttackVfx.Kind>())
            {
                AddChild(new Label { Text=kind.ToString(),Position=new Vector2(i*400+20,20) });
                AddChild(new GuardianStormAttackVfx { AttackKind=kind,Position=new Vector2(i++*400+200,275) });
            }
            return;
        }
        if(Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--special"))
        {
            foreach(var kind in Enum.GetValues<GuardianSpecialAttackVfx.Kind>())
            {
                AddChild(new Label { Text=kind.ToString(),Position=new Vector2(i*300+20,20) });
                AddChild(new GuardianSpecialAttackVfx { AttackKind=kind,Position=new Vector2(i++*300+150,290) });
            }
            return;
        }
        foreach(var kind in Enum.GetValues<GuardianWindVfx.Kind>())
        {
            AddChild(new Label { Text=kind.ToString(),Position=new Vector2(i*400+20,20) });
            AddChild(new GuardianWindVfx { WindKind=kind,Position=new Vector2(i++*400+200,300) });
        }
    }
    public override async void _Process(double delta)
    {
        _age+=delta;
        if (_revised && !_impactStarted && _age>=1.625)
        {
            _impactStarted=true;
            AddChild(new GuardianSpecialAttackVfx { AttackKind=GuardianSpecialAttackVfx.Kind.Heavenfall, Position=new Vector2(1000,290), Scale=Vector2.One*.45f });
        }
        float[] frames=_revised ? new[]{.24f,.75f,1.8f,2.1f,2.8f} : new[]{.16f,.32f,.48f};
        var args=OS.GetCmdlineUserArgs();
        if(args.Length>0 && !_capturing && _capture<frames.Length && _age>=frames[_capture])
        {
            _capturing=true;
            int frame=_capture++;
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            DirAccess.MakeDirRecursiveAbsolute(args[0]);
            GetViewport().GetTexture().GetImage().SavePng(args[0]+$"/wind_{frame}.png");
            _capturing=false;
        }
        if(_age>(_revised?4.2:2.5))
        {
            int remaining=0;
            foreach(Node child in GetChildren()) if(child is GuardianWindVfx or GuardianSpecialAttackVfx or GuardianStormAttackVfx or GuardianHeavenfallPreludeVfx) remaining++;
            GD.Print($"GUARDIAN_WIND_LIFECYCLE remaining={remaining}");
            GetTree().Quit(remaining==0?0:1);
        }
    }
}
