using System;
using Godot;
using NightMustStay.Core.Nodes.Vfx;

public partial class Preview : Node2D
{
 private float _age;
 private int _capture;
 private readonly float[] _samples={.16f,.32f,.52f,.72f};
 public override void _Ready()
 {
  ParticleVfxMaterials.AssetRoot=ProjectSettings.GlobalizePath("res://../../images/vfx/particle_remake/");
  if(Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--reverse-time"))
  {
   AddChild(new DuchessReverseTimeVfx());
   return;
  }
  foreach(var kind in Enum.GetValues<DuchessAttackVfx.Kind>())
  {
   int index=(int)kind;
   var label=new Label { Text=kind.ToString(), Position=new Vector2(index%4*360+12,index/4*320+12) };
   AddChild(label);
   AddChild(new DuchessAttackVfx { AttackKind=kind, Source=new Vector2(-140,0), Position=new Vector2(index%4*360+205,index/4*320+175),Scale=new Vector2(.63f,.63f) });
  }
 }
 public override async void _Process(double delta)
 {
  _age+=(float)delta;
  bool reverse=Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--reverse-time");
  float[] samples=reverse ? new[]{.45f,1.05f,1.65f,2.05f} : _samples;
  if(_capture<samples.Length && _age>=samples[_capture])
  {
   int frame=_capture++;
   await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   var args=OS.GetCmdlineUserArgs();
   if(args.Length>0) { DirAccess.MakeDirRecursiveAbsolute(args[0]); GetViewport().GetTexture().GetImage().SavePng(args[0]+$"/attacks_{frame}.png"); }
  }
  if(_age>(reverse?2.6f:1.2f))
  {
   int remaining=0;foreach(Node node in GetChildren())if(node is DuchessAttackVfx)remaining++;
   GD.Print($"DUCHESS_VFX_LIFECYCLE remaining={remaining}");
   GetTree().Quit(remaining==0?0:1);
  }
 }
}
