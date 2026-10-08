#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>One presentation instance per native hit. No waits, random state or gameplay effects.</summary>
public partial class GuardianStormAttackVfx : CombatVfxCanvas
{
    public enum Kind { InvokeStorm, StormAssault, CycloneHalberd }
    public Kind AttackKind { get; set; }
    public Action? OnStart { get; set; }
    public float Duration => AttackKind == Kind.InvokeStorm ? .42f : AttackKind == Kind.StormAssault ? .48f : .6f;
    private float _age;
    private bool _manual;
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
        float alpha = Ease(0, .06f, t) * (1 - Ease(.58f, 1, t));
        if (alpha <= .001f) return;
        Color air = Fade(new Color("9ECBD3"), alpha * .65f);
        Color core = Fade(new Color("EDF5DF"), alpha);
        if (AttackKind == Kind.InvokeStorm)
        {
            // Three expanding, tilted pressure crescents: a short pulse per X hit.
            for (int band = 0; band < 3; band++)
            {
                float radius = 35 + t * 145 + band * 18;
                for (int i = 0; i < _points.Length; i++)
                {
                    float u = i / (float)(_points.Length - 1);
                    float a = t * 7 + band * 2.1f + u * MathF.PI * 1.6f;
                    _points[i] = new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * .48f).Rotated((band - 1) * .35f);
                }
                Ribbon(air, 14); Ribbon(core, 2.5f);
            }
            Splinters(t, Vector2.Zero, air, 7, 110);
            return;
        }
        if (AttackKind == Kind.StormAssault)
        {
            float head = -230 + Ease(0, .6f, t) * 260 + t * 35;
            for (int lane = 0; lane < 4; lane++)
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    float u = i / (float)(_points.Length - 1);
                    _points[i] = new Vector2((lane - 1.5f) * 32 * (1 - u * .65f)
                        + MathF.Sin(u * 5 + t * 8 + lane) * 18,
                        head - 170 + u * 190);
                }
                Ribbon(air, 13); Ribbon(core, 3);
            }
            // Continuous shaft and asymmetric hooked spearhead descend with the air.
            Vector2 fallingTip = new(0, head + 25);
            DrawLine(fallingTip - new Vector2(0, 160), fallingTip - new Vector2(0, 25), core, 5, true);
            Shard(fallingTip, 42, 5, MathF.PI * .5f, core);
            Poly(core, fallingTip + new Vector2(8, -30), fallingTip + new Vector2(39, -58),
                fallingTip + new Vector2(48, -105), fallingTip + new Vector2(17, -82), fallingTip + new Vector2(20, -52));
            if (t > .45f) Splinters((t - .45f) / .55f, new Vector2(0, 55), air, 8, 110);
            return;
        }
        // A continuously rotating physical halberd head leads two broad wind arcs.
        Vector2 pivot = new(-55, 45);
        float angle = -2.4f + t * 5.1f;
        Vector2 d = Vector2.FromAngle(angle), n = d.Orthogonal();
        Vector2 tip = pivot + d * 160;
        Color steel = Fade(new Color("D5E1E5"), alpha);
        DrawLine(pivot - d * 90, tip, steel, 5, true);
        // Axial spear, large hooked wing on one side, small opposite spur, open crescent.
        Shard(tip + d * 24, 42, 5, angle, steel);
        Poly(steel, tip - d * 18 + n * 12, tip - d * 42 + n * 43,
            tip - d * 94 + n * 58, tip - d * 69 + n * 21, tip - d * 40 + n * 25);
        Poly(steel, tip - d * 20 - n * 8, tip - d * 38 - n * 27, tip - d * 51 - n * 7);
        for (int band = 0; band < 2; band++)
        {
            for (int i = 0; i < _points.Length; i++)
            {
                float u = i / (float)(_points.Length - 1);
                float a = angle - 2.3f + u * 2.3f - band * .25f;
                _points[i] = pivot + new Vector2(MathF.Cos(a) * (190 - band * 30), MathF.Sin(a) * (120 - band * 15));
            }
            Ribbon(air, 18); Ribbon(core, 3);
        }
    }
    private void Ribbon(Color color, float width)
    {
        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = Fade(color, color.A * MathF.Sin(i * MathF.PI / (_colors.Length - 1)));
        DrawPolylineColors(_points, _colors, width, true);
    }
}
