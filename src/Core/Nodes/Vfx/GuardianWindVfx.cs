#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Target-local wind, independent of combat state; continuous motion and bounded lifetime.</summary>
public partial class GuardianWindVfx : CombatVfxCanvas
{
    public enum Kind { GreatTornado, Cyclone, Whirlwind }
    public Kind WindKind { get; set; }
    public Action? OnStart { get; set; }
    public float Duration => WindKind == Kind.GreatTornado ? .9f : WindKind == Kind.Cyclone ? .55f : .65f;
    private float _age;
    private bool _manual;
    // Reused vertices; single polyline per ribbon, no per-frame polygon tessellation.
    private readonly Vector2[] _points = new Vector2[33];
    private readonly Color[] _colors = new Color[33];
    public override void _Ready() { OnStart?.Invoke(); OnStart = null; }
    public override void _Process(double delta)
    {
        if (_manual) return;
        _age += (float)delta;
        if (_age >= Duration) { QueueFree(); return; }
        QueueRedraw();
    }
    public void Seek(float progress) { _manual = true; _age = Math.Clamp(progress, 0, 1) * Duration; QueueRedraw(); }
    public override void _Draw()
    {
        float t = Math.Clamp(_age / Duration, 0, 1);
        float alpha = Ease(0, .08f, t) * (1 - Ease(.65f, 1, t));
        if (alpha <= .001f) return;
        Color edge = Fade(new Color("85C9D7"), alpha * .6f);
        Color core = Fade(new Color("E6F4EA"), alpha * .9f);
        int bands = WindKind == Kind.GreatTornado ? 7 : WindKind == Kind.Cyclone ? 4 : 3;
        for (int band = 0; band < bands; band++)
        {
            float phase = t * MathF.Tau * 2.2f + band * 1.35f;
            for (int i = 0; i < _points.Length; i++)
            {
                float u = i / (float)(_points.Length - 1);
                float a = phase + u * MathF.PI * 1.35f;
                if (WindKind == Kind.GreatTornado)
                {
                    float height = (band + u) / bands;
                    float radius = 22 + height * 100;
                    _points[i] = new Vector2(MathF.Cos(a) * radius, 95 - height * 305 - t * 30 + MathF.Sin(a) * radius * .18f);
                }
                else if (WindKind == Kind.Cyclone)
                {
                    float radius = (138 - band * 21) * (1 - .25f * t);
                    _points[i] = new Vector2(MathF.Cos(a) * radius, 28 + MathF.Sin(a) * radius * .35f - band * 12);
                }
                else
                {
                    float radius = 160 + t * 80 - band * 20;
                    _points[i] = new Vector2(MathF.Cos(a) * radius + t * 45, MathF.Sin(a) * radius * .27f + (band - 1) * 22);
                }
            }
            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = Fade(edge, edge.A * MathF.Sin(i * MathF.PI / (_colors.Length - 1)));
            DrawPolylineColors(_points, _colors, WindKind == Kind.Whirlwind ? 17 : 12, true);
            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = Fade(core, core.A * MathF.Sin(i * MathF.PI / (_colors.Length - 1)));
            DrawPolylineColors(_points, _colors, 3, true);
        }
        // A few drifting feather-like chips communicate air, not liquid or flame.
        for (int i = 0; i < 5; i++)
        {
            float a = t * 8 + i * MathF.Tau / 5;
            float radius = WindKind == Kind.Whirlwind ? 170 + t * 75 : 65 + i * 13;
            Vector2 p = new(MathF.Cos(a) * radius, MathF.Sin(a) * 40);
            if (WindKind == Kind.GreatTornado) p.Y -= (t * 230 + i * 39) % 240;
            Shard(p, 9, 2, a + MathF.PI * .5f, Fade(core, alpha * .55f));
        }
    }
}
