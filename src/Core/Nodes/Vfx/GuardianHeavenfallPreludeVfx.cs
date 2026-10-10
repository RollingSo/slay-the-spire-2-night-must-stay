using Godot;
using System;

namespace NightMustStay.Core.Nodes.Vfx;

public partial class GuardianHeavenfallPreludeVfx : CombatVfxCanvas
{
    private float _age;
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= 1.625f) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float t = _age / 1.625f;
        Color blue = Fade(new Color("56CBEC"), Ease(0, .2f, t) * .3f);
        Poly(blue, new Vector2(-160, -380), new Vector2(160, -380), new Vector2(45, 100), new Vector2(-45, 100));
        for (int i = 0; i < 8; i++)
        {
            float y = 95 - ((t * 400 + i * 52) % 420);
            float x = (i % 2 == 0 ? -1 : 1) * (30 + i * 12);
            Shard(new Vector2(x, y), 25, 4, -MathF.PI * .5f, Fade(blue, blue.A * 1.7f));
        }
        DrawArc(new Vector2(0, 90), 110 - t * 55, 0, MathF.Tau, 32, blue, 4, true);
    }
}

/// <summary>Owns both native impact and custom accent without leaving an empty wrapper.</summary>
public partial class GuardianHeavenfallImpactContainer : Node2D
{
    public Node2D Effect { get; set; } = null!;
    private float _age;
    public override void _Ready() { AddChild(Effect); }
    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= 2.2f) QueueFree();
    }
}
