using System;
using System.Diagnostics;
using System.Reflection;
using Godot;
using NightMustStay.Core.Nodes.Vfx;

// Baselines snapshot the immediately preceding production sources, not the older
// October 16 optimization. Native Grand Finale VFX are deliberately not replaced.
public partial class TimeWaltzEquivalence : Node
{
    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport { Size = new Vector2I(1440, 960), TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            AddChild(viewport);
            int samples = 0;
            foreach (Vector2I size in new[] { new Vector2I(1440, 960), new Vector2I(1920, 1080), new Vector2I(960, 720) })
            {
                viewport.Size = size;
                foreach (float t in new[] { 0f, .001f, .08f, .24f, .42f, .58f, .676f, .678f, .7f, .78f, .95f, 1f })
                {
                    var before = new OctoberReverseTimeVfx(); before.Seek(t);
                    var after = new DuchessReverseTimeVfx(); after.Seek(t);
                    await Compare(before, after, $"ReverseTime {size} {t}");
                    foreach (bool impact in new[] { false, true })
                    {
                        Vector2 focus = new(size.X * .52f, size.Y * .48f);
                        var oldWaltz = new OctoberMidnightWaltzVfx(); oldWaltz.Seek(t, impact, focus);
                        var newWaltz = new DuchessMidnightWaltzVfx(); newWaltz.Seek(t, impact, focus);
                        await Compare(oldWaltz, newWaltz, $"Waltz impact={impact} {size} {t}");
                    }
                }
                foreach (float t in new[] { .24f, .78f, .95f })
                {
                    var oldGroup = new Node2D(); var newGroup = new Node2D();
                    for (int target = 0; target < 5; target++)
                    {
                        Vector2 focus = new(size.X * (.32f + target * .09f), size.Y * (.43f + target % 2 * .1f));
                        var oldImpact = new OctoberMidnightWaltzVfx(); oldImpact.Seek(t, true, focus);
                        var newImpact = new DuchessMidnightWaltzVfx(); newImpact.Seek(t, true, focus);
                        oldGroup.AddChild(oldImpact); newGroup.AddChild(newImpact);
                    }
                    await Compare(oldGroup, newGroup, $"Waltz five overlapping targets {size} {t}");
                }
            }
            GD.Print($"TIME_WALTZ_PIXEL_EQUIVALENCE_OK samples={samples}");
            viewport.Size = new Vector2I(1440, 960);
            await Measure(new TimedOldReverse(), new TimedReverse(), "ReverseTime");
            await Measure(new TimedOldWaltz(), new TimedWaltz(), "WaltzPrelude");
            await Measure(new TimedOldWaltz { Impact = true }, new TimedWaltz { Impact = true }, "WaltzImpact");

            var layer = new CanvasLayer(); viewport.AddChild(layer);
            var reverse = new DuchessReverseTimeVfx { CleanupLayer = true }; layer.AddChild(reverse);
            var prelude = new DuchessMidnightWaltzVfx(); viewport.AddChild(prelude);
            var impactNode = new DuchessMidnightWaltzVfx(); impactNode.Seek(0f, true, new Vector2(720, 480));
            typeof(DuchessMidnightWaltzVfx).GetField("_manual", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(impactNode, false);
            viewport.AddChild(impactNode);
            await ToSignal(GetTree().CreateTimer(2.6), SceneTreeTimer.SignalName.Timeout);
            if (GodotObject.IsInstanceValid(layer) || GodotObject.IsInstanceValid(reverse) ||
                GodotObject.IsInstanceValid(prelude) || GodotObject.IsInstanceValid(impactNode))
                throw new Exception("Natural-playback VFX lifecycle leak");
            GD.Print("TIME_WALTZ_LIFECYCLE_OK remaining=0 (reverse layer, prelude, impact)");
            GetTree().Quit();

            async System.Threading.Tasks.Task Measure(Node2D before, Node2D after, string label)
            {
                // Repeat across frames; a median avoids attributing an isolated
                // driver/JIT/OS scheduling stall to the draw implementation.
                var beforeMs = new double[3]; var afterMs = new double[3];
                for (int round = 0; round < 3; round++)
                {
                    viewport.AddChild(before); before.QueueRedraw();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    beforeMs[round] = DrawBenchmark.BeforeMs;
                    viewport.RemoveChild(before);
                    viewport.AddChild(after); after.QueueRedraw();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    afterMs[round] = DrawBenchmark.AfterMs;
                    viewport.RemoveChild(after);
                }
                before.Free(); after.Free();
                Array.Sort(beforeMs); Array.Sort(afterMs);
                GD.Print($"TIME_WALTZ_DRAW_CPU {label} iterations={DrawBenchmark.Iterations} rounds=3 median " +
                    $"before_ms={beforeMs[1]:F3} after_ms={afterMs[1]:F3} " +
                    $"before_bytes={DrawBenchmark.BeforeBytes} after_bytes={DrawBenchmark.AfterBytes}");
                if (DrawBenchmark.AfterBytes > DrawBenchmark.BeforeBytes) throw new Exception($"Allocation regression: {label}");
            }

            async System.Threading.Tasks.Task Compare(Node2D before, Node2D after, string label)
            {
                viewport.AddChild(before);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image expectedImage = viewport.GetTexture().GetImage();
                byte[] expected = expectedImage.GetData();
                viewport.RemoveChild(before); before.Free();
                viewport.AddChild(after);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image actualImage = viewport.GetTexture().GetImage();
                byte[] actual = actualImage.GetData();
                if (!expected.AsSpan().SequenceEqual(actual))
                {
                    int changed = 0, maximum = 0;
                    for (int i = 0; i < expected.Length; i++)
                        if (expected[i] != actual[i]) { changed++; maximum = Math.Max(maximum, Math.Abs(expected[i] - actual[i])); }
                    throw new Exception($"Pixel mismatch {label}: channels={changed}, maxDelta={maximum}");
                }
                viewport.RemoveChild(after); after.Free(); samples++;
            }
        }
        catch (Exception e) { GD.PrintErr(e); GetTree().Quit(1); }
    }
}

internal static class DrawBenchmark
{
    internal const int Iterations = 240;
    internal static double BeforeMs, AfterMs;
    internal static long BeforeBytes, AfterBytes;
    internal static void Run(Node2D canvas, Action draw, bool before)
    {
        long ticks = 0, bytes = 0;
        for (int i = -16; i < Iterations; i++)
        {
            RenderingServer.CanvasItemClear(canvas.GetCanvasItem());
            long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
            draw();
            long elapsed = Stopwatch.GetTimestamp() - start;
            if (i >= 0) { ticks += elapsed; bytes += GC.GetAllocatedBytesForCurrentThread() - allocated; }
        }
        if (before) { BeforeMs = ticks * 1000d / Stopwatch.Frequency; BeforeBytes = bytes; }
        else { AfterMs = ticks * 1000d / Stopwatch.Frequency; AfterBytes = bytes; }
    }
}
public partial class TimedOldReverse : OctoberReverseTimeVfx
{
    public override void _Ready() { Seek(.78f); }
    public override void _Draw() => DrawBenchmark.Run(this, DrawOne, true);
    private void DrawOne() => base._Draw();
}
public partial class TimedReverse : DuchessReverseTimeVfx
{
    public override void _Ready() { Seek(.78f); }
    public override void _Draw() => DrawBenchmark.Run(this, DrawOne, false);
    private void DrawOne() => base._Draw();
}
public partial class TimedOldWaltz : OctoberMidnightWaltzVfx
{
    public bool Impact;
    public override void _Ready() { Seek(.42f, Impact, new Vector2(720, 480)); }
    public override void _Draw() => DrawBenchmark.Run(this, DrawOne, true);
    private void DrawOne() => base._Draw();
}
public partial class TimedWaltz : DuchessMidnightWaltzVfx
{
    public bool Impact;
    public override void _Ready() { Seek(.42f, Impact, new Vector2(720, 480)); }
    public override void _Draw() => DrawBenchmark.Run(this, DrawOne, false);
    private void DrawOne() => base._Draw();
}
