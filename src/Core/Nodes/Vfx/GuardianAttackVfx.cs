#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Three texture-free silhouettes, shared by combat and the visual test scene.</summary>
public partial class GuardianAttackVfx : Node2D
{
    public enum Kind { Weapon, Whirlwind, Counter }

    public Kind AttackKind { get; set; }
    public Action? OnStart { get; set; }
    public float Duration => AttackKind switch { Kind.Weapon => 0.36f, Kind.Whirlwind => 0.54f, _ => 0.50f };
    private float _age;
    private static readonly Color Ink = new("#142C3C");
    private static readonly Color Steel = new("#DCECF1");
    private static readonly Color White = new("#FAF8E7");
    private static readonly Color Wind = new("#70BFCE");
    private static readonly Color Gold = new("#D7A45D");

    public override void _Ready()
    {
        OnStart?.Invoke();
        OnStart = null;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Duration) QueueFree();
        else QueueRedraw();
    }

    public override void _Draw() => DrawFrame(Math.Clamp(_age / Duration, 0f, 1f));

    // Public so the test renderer uses the actual production geometry, not an imitation.
    public void DrawFrame(float t)
    {
        float alpha = 1f - Smooth(0.48f, 1f, t);
        if (alpha <= 0f) return;
        switch (AttackKind)
        {
            case Kind.Weapon: DrawWeapon(t, alpha); break;
            case Kind.Whirlwind: DrawWind(t, alpha); break;
            case Kind.Counter: DrawCounter(t, alpha); break;
        }
    }

    private void DrawWeapon(float t, float alpha)
    {
        float sweep = 1f - MathF.Pow(1f - Math.Min(t * 2.4f, 1f), 3f);
        // One broad cutting plane, not a glowing sword or a particle cloud.
        float end = -0.20f + sweep * 1.32f;
        float tail = Smooth(0.32f, 1f, t) * 2.8f;
        Ribbon(new(-20, 10), new(152, 66), -2.65f + tail, end, 29, -0.47f, Tint(Ink, alpha));
        Ribbon(new(-18, 8), new(147, 61), -2.62f + tail, end, 21, -0.47f, Tint(Steel, alpha));
        Ribbon(new(-18, 8), new(148, 61), -2.1f + tail * 0.8f, end, 5, -0.47f, Tint(White, alpha));
        float hit = (1f - Smooth(0.18f, 0.58f, t));
        Shard(Vector2.Zero, 39 * hit, 8 * hit, -0.5f, Tint(White, alpha));
        Debris(t, alpha, Steel, 4, -0.6f, 25, 66);
    }

    private void DrawWind(float t, float alpha)
    {
        float turn = -0.65f + t * 2.45f;
        float spread = 0.78f + 0.27f * Smooth(0, 0.6f, t);
        // Flattened, open wing-shaped bands; no vertical funnel or noisy vortex.
        Ribbon(new(0, -24), new Vector2(160, 53) * spread, turn - 3.2f, turn, 25, -0.13f, Tint(Ink, alpha * 0.9f));
        Ribbon(new(0, -27), new Vector2(156, 49) * spread, turn - 3.2f, turn, 17, -0.13f, Tint(Wind, alpha));
        Ribbon(new(0, -27), new Vector2(156, 49) * spread, turn - 2.55f, turn, 4, -0.13f, Tint(White, alpha));
        Ribbon(new(4, 20), new Vector2(127, 40) * spread, turn + 0.35f, turn + 3.2f, 19, 0.16f, Tint(Ink, alpha));
        Ribbon(new(4, 17), new Vector2(124, 37) * spread, turn + 0.35f, turn + 3.2f, 12, 0.16f, Tint(Steel, alpha));
        Ribbon(new(-8, 47), new Vector2(83, 24) * spread, turn - 2.8f, turn - 0.15f, 8, -0.1f, Tint(Wind, alpha * 0.7f));
        // Exactly three feather-like slivers track the wind; no random emission.
        for (int i = 0; i < 3; i++)
        {
            float a = turn + i * 2.1f;
            Vector2 p = new(MathF.Cos(a) * (140 + i * 8), MathF.Sin(a) * 63 - 9);
            Shard(p, 18 - i * 3, 4, a + MathF.PI / 2, Tint(Steel, alpha));
        }
    }

    private void DrawCounter(float t, float alpha)
    {
        // Block cue yields immediately to a heavy halberd cut. Never a shield bash.
        float brace = 1f - Smooth(0.04f, 0.26f, t);
        Ribbon(new(-39, 0), new(47, 60), 2.0f, 4.2f, 10, 0, Tint(Steel, brace));
        float strike = Smooth(0.015f, 0.16f, t);
        float travel = 1f - MathF.Pow(1f - Math.Min(t * 3.5f, 1f), 3f);
        float tail = Smooth(0.4f, 1f, t) * 3.1f;
        Ribbon(new(-14, -7), new(147, 73), -2.7f + tail, -1.3f + travel * 3.2f, 38, 0.58f, Tint(Ink, alpha * strike));
        Ribbon(new(-12, -9), new(141, 68), -2.65f + tail, -1.3f + travel * 3.2f, 27, 0.58f, Tint(Gold, alpha * strike));
        Ribbon(new(-12, -9), new(142, 68), -2.2f + tail * 0.85f, -1.3f + travel * 3.2f, 9, 0.58f, Tint(White, alpha * strike));
        float punch = strike * (1f - Smooth(0.25f, 0.75f, t));
        Shard(Vector2.Zero, 63 * punch, 15 * punch, 0.58f, Tint(White, alpha));
        Shard(Vector2.Zero, 31 * punch, 7 * punch, 2.15f, Tint(Gold, alpha));
        Debris(t, alpha * strike, Gold, 5, 0.55f, 29, 92);
    }

    private void Ribbon(Vector2 center, Vector2 radius, float start, float end, float width, float rotation, Color color)
    {
        if (color.A <= 0.001f || width <= 0) return;
        const int segments = 28;
        // Separate triangle strips avoid self-intersecting concave polygons.
        Vector2 Outer(float u) => center + new Vector2(MathF.Cos(Mathf.Lerp(start, end, u)) * radius.X,
            MathF.Sin(Mathf.Lerp(start, end, u)) * radius.Y).Rotated(rotation);
        Vector2 Inner(float u)
        {
            float a = Mathf.Lerp(start, end, u);
            float w = width * MathF.Pow(Math.Max(0, MathF.Sin(u * MathF.PI)), 0.7f);
            return center + new Vector2(MathF.Cos(a) * (radius.X - w), MathF.Sin(a) * (radius.Y - w)).Rotated(rotation);
        }
        for (int i = 0; i < segments; i++)
        {
            float u = i / (float)segments, v = (i + 1) / (float)segments;
            DrawColoredPolygon(new[] { Outer(u), Outer(v), Inner(v) }, color);
            DrawColoredPolygon(new[] { Outer(u), Inner(v), Inner(u) }, color);
        }
    }

    private void Debris(float t, float alpha, Color color, int count, float direction, float start, float distance)
    {
        float fade = alpha * (1f - Smooth(0.35f, 0.85f, t));
        for (int i = 0; i < count; i++)
        {
            float angle = direction + (i - (count - 1) * 0.5f) * 0.67f;
            Vector2 position = Vector2.FromAngle(angle) * (start + distance * t);
            Shard(position, (12 - i) * (1 - t), 2.5f * (1 - t), angle, Tint(color, fade));
        }
    }

    private void Shard(Vector2 center, float length, float width, float angle, Color color)
    {
        if (length <= 0.01f || color.A <= 0.001f) return;
        Vector2 along = Vector2.FromAngle(angle), normal = along.Orthogonal();
        DrawColoredPolygon(new[] { center - along * length, center + normal * width,
            center + along * length, center - normal * width }, color);
    }

    private static Color Tint(Color color, float alpha) => new(color.R, color.G, color.B, Math.Clamp(alpha, 0, 1));
    private static float Smooth(float a, float b, float t)
    {
        t = Math.Clamp((t - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
