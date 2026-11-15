using System;
using System.IO;
using Godot;
using NightMustStay.Core.Nodes.Vfx;

// Only game data/cache are stubbed. Controls, frame lifecycle and drawing use
// the real Godot engine and the linked production presentation class.
namespace MegaCrit.Sts2.Core.Entities.Players
{
    public sealed class Player { public object Creature { get; set; } }
}
namespace NightMustStay.Core.Models.Power
{
    public static class WylderPursuitPower
    {
        public static bool IsActive(object creature) => creature is true;
    }
}
namespace MegaCrit.Sts2.Core.Assets
{
    public static class PreloadManager
    {
        public static class Cache
        {
            public static Texture2D GetTexture2D(string path)
            {
                string root = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../.."));
                return ImageTexture.CreateFromImage(Image.LoadFromFile(Path.Combine(root, path.Substring(6))));
            }
        }
    }
}

public partial class PursuitUiTests : Node
{
    public override async void _Ready()
    {
        try
        {
            var left = CreateCounter(new Vector2(110, 110));
            var right = CreateCounter(new Vector2(400, 110));
            var player = new MegaCrit.Sts2.Core.Entities.Players.Player { Creature = false };
            var visual = new WylderPursuitEnergyVisual { Player = player };
            var other = new WylderPursuitEnergyVisual { Player = new() { Creature = false } };
            left.AddChild(visual);
            right.AddChild(other);
            var baseLayer = left.GetNode<TextureRect>("Layers/Layer1");
            var normal = baseLayer.Texture;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(baseLayer.Texture == normal, "inactive creation");
            player.Creature = true;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            visual._Process(0.1);
            Check(baseLayer.Texture != normal, "orb texture replaced");
            Check(!left.GetNode<CanvasItem>("Layers/RotationLayers").Visible, "old frame hidden");
            var water = left.GetNode<TextureRect>("Layers/PursuitWater");
            Check(water.Visible && water.GetIndex() > baseLayer.GetIndex(), "complete water above background");
            Check(right.GetNode<CanvasItem>("Layers/RotationLayers").Visible, "other counter unaffected");
            Check(visual.MouseFilter == Control.MouseFilterEnum.Ignore, "mouse passes through");
            Check(visual.GetChildCount() == 0, "no external decorations");
            Check(baseLayer.Size == left.Size, "original orb footprint retained");
            if (DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("res://preview.png");
            }
            player.Creature = false;
            visual._Process(0);
            Check(baseLayer.Texture == normal && left.GetNode<CanvasItem>("Layers/RotationLayers").Visible && !water.Visible, "normal art restored");
            player.Creature = true;
            visual.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var restored = new WylderPursuitEnergyVisual { Player = player };
            left.AddChild(restored);
            Check(baseLayer.Texture != normal, "active recreation / restored fight");
            left.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!GodotObject.IsInstanceValid(restored), "freed with counter");
            GD.Print("PASS: pursuit UI entry, exit, recreation, independent counters, mouse, bounds and cleanup");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private Control CreateCounter(Vector2 position)
    {
        var counter = new Control { Position = position, Size = new Vector2(128,128) };
        AddChild(counter);
        var layers = new Control { Name = "Layers", Size = counter.Size };
        counter.AddChild(layers);
        var rotating = new Control { Name = "RotationLayers", Size = counter.Size };
        layers.AddChild(rotating);
        for (int i = 1; i <= 5; i++)
            (i is 2 or 3 ? rotating : layers).AddChild(new TextureRect {
                Name = $"Layer{i}",
                Size = counter.Size, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Texture = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetTexture2D($"res://wylder_assets/energy_counter/wylder_orb_layer_{i}.png") });
        var label = new Label { Text = "2/3", Size = counter.Size,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Theme = null };
        label.AddThemeFontSizeOverride("font_size", 36);
        label.AddThemeConstantOverride("outline_size", 6);
        label.AddThemeColorOverride("font_outline_color", new Color("172439"));
        counter.AddChild(label);
        return counter;
    }
    private static void Check(bool condition, string name)
    { if (!condition) throw new InvalidOperationException("FAIL: " + name); }
}
