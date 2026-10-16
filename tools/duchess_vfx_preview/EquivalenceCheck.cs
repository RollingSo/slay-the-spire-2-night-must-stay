using System;
using Godot;
using NightMustStay.Core.Nodes.Vfx;

public partial class EquivalenceCheck : Node
{
    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport { Size = new Vector2I(1440,960), TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            AddChild(viewport);
            int samples=0;
            foreach (DuchessAttackVfx.Kind kind in Enum.GetValues<DuchessAttackVfx.Kind>())
            {
                foreach(float t in new[]{.08f,.24f,.42f,.58f,.78f,.95f})
                {
                    var before=new MeasuredBaselineAttack { AttackKind=(BaselineDuchessAttackVfx.Kind)(int)kind,
                        Position=new Vector2(720,480),Source=new Vector2(-300,0) };
                    before.Seek(t);viewport.AddChild(before);
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    byte[] expected=viewport.GetTexture().GetImage().GetData();
                    viewport.RemoveChild(before);before.Free();
                    var after=new MeasuredAttack { AttackKind=kind,Position=new Vector2(720,480),Source=new Vector2(-300,0) };
                    after.Seek(t);viewport.AddChild(after);
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    byte[] actual=viewport.GetTexture().GetImage().GetData();
                    if(!expected.AsSpan().SequenceEqual(actual))throw new Exception($"Pixel mismatch {kind} {t}");
                    viewport.RemoveChild(after);after.Free();samples++;
                }
            }
            foreach(float t in new[]{.08f,.24f,.42f,.58f,.78f,.95f})
            {
                var before=new MeasuredBaselineReverse();before.Seek(t);viewport.AddChild(before);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                byte[] expected=viewport.GetTexture().GetImage().GetData();
                viewport.RemoveChild(before);before.Free();
                var after=new MeasuredReverse();after.Seek(t);viewport.AddChild(after);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                byte[] actual=viewport.GetTexture().GetImage().GetData();
                if(!expected.AsSpan().SequenceEqual(actual))throw new Exception($"Pixel mismatch ReverseTime {t}");
                viewport.RemoveChild(after);after.Free();samples++;
            }
            GD.Print($"DUCHESS_VFX_PIXEL_EQUIVALENCE_OK samples={samples}");
            GD.Print($"DRAW_ALLOCATIONS baseline={DrawAllocation.Before} optimized={DrawAllocation.After}");
            if(DrawAllocation.After>=DrawAllocation.Before)throw new Exception("Draw allocation regression");
            GetTree().Quit();
        }
        catch(Exception e) { GD.PrintErr(e);GetTree().Quit(1); }
    }
}

public static class DrawAllocation { public static long Before,After; }
public partial class MeasuredBaselineAttack : BaselineDuchessAttackVfx
{
    public override void _Draw() { long start=GC.GetAllocatedBytesForCurrentThread();base._Draw();DrawAllocation.Before+=GC.GetAllocatedBytesForCurrentThread()-start; }
}
public partial class MeasuredAttack : DuchessAttackVfx
{
    public override void _Draw() { long start=GC.GetAllocatedBytesForCurrentThread();base._Draw();DrawAllocation.After+=GC.GetAllocatedBytesForCurrentThread()-start; }
}
public partial class MeasuredBaselineReverse : BaselineDuchessReverseTimeVfx
{
    public override void _Draw() { long start=GC.GetAllocatedBytesForCurrentThread();base._Draw();DrawAllocation.Before+=GC.GetAllocatedBytesForCurrentThread()-start; }
}
public partial class MeasuredReverse : DuchessReverseTimeVfx
{
    public override void _Draw() { long start=GC.GetAllocatedBytesForCurrentThread();base._Draw();DrawAllocation.After+=GC.GetAllocatedBytesForCurrentThread()-start; }
}
