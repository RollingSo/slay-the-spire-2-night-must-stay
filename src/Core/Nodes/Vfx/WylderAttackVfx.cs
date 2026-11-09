#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Short-lived, allocation-light procedural hit graphics for Wylder.</summary>
public partial class WylderAttackVfx : Node2D
{
    public enum Kind
    {
        Greatsword, Claw, FireExplosion, FlameSlash, StormBlade, VacuumSlice,
        CorpseWax, OrdovisVortex, SiluriaVortex, TakersFlames, GhostflameExplosion
    }

    private static readonly Color Steel = new("EAF2FF");
    private static readonly Color Wind = new("BDEBFF");
    private static readonly Color Gold = new("FFD36A");
    private static readonly Color Flame = new("FF6A22");
    private static readonly Color Red = new("B61F35");
    private static readonly Color Ghost = new("77E8E2");
    private float _age;

    public Kind AttackKind { get; set; }
    public Vector2 Origin { get; set; } = new(-260f, 0f);
    public float Duration => AttackKind == Kind.FireExplosion ? .82f : AttackKind == Kind.GhostflameExplosion ? .78f : .62f;

    public override void _Ready()
    {
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Duration) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_age / Duration, 0f, 1f);
        float fade = 1f - Mathf.SmoothStep(.45f, 1f, t);
        switch (AttackKind)
        {
            case Kind.Greatsword: DrawSlash(t, fade, Steel, 180f, -.55f); break;
            case Kind.Claw: DrawClaw(t, fade); break;
            case Kind.FireExplosion: DrawBurst(t, fade, Flame, Gold, 14); break;
            case Kind.FlameSlash: DrawSlash(t, fade, Flame, 215f, -.35f); DrawArc(Vector2.Zero, 125f + 60f * t, -2.55f, .25f, 30, new Color(Gold, .75f * fade), 8f, true); break;
            case Kind.StormBlade: DrawWave(t, fade, Wind, 3, 115f); break;
            case Kind.VacuumSlice: DrawWave(t, fade, Steel, 1, 205f); break;
            case Kind.CorpseWax: DrawWave(t, fade, new Color(Red, .95f), 2, 175f); break;
            case Kind.OrdovisVortex: DrawVortex(t, fade, Gold, false); break;
            case Kind.SiluriaVortex: DrawVortex(t, fade, new Color("F6C56B"), true); break;
            case Kind.TakersFlames: DrawBurst(t, fade, Red, Flame, 9); DrawArc(Vector2.Zero, 75f + 95f * t, -2.8f, .25f, 34, new Color(Flame, fade), 13f, true); break;
            case Kind.GhostflameExplosion: DrawBurst(t, fade, Ghost, Wind, 11); break;
        }
    }

    private void DrawSlash(float t, float fade, Color color, float reach, float angle)
    {
        Vector2 d = Vector2.FromAngle(angle);
        Vector2 n = new(-d.Y, d.X);
        float r = Mathf.Lerp(40f, reach, Mathf.Sqrt(t));
        DrawLine(-d * r - n * 28f, d * r + n * 28f, new Color(color, .28f * fade), 25f, true);
        DrawLine(-d * r, d * r, new Color(color, fade), 8f, true);
        DrawCircle(d * r, 8f, new Color(Steel, fade));
    }

    private void DrawClaw(float t, float fade)
    {
        float flight = Mathf.Clamp(t / .34f, 0f, 1f);
        Vector2 tip = Origin.Lerp(Vector2.Zero, Mathf.SmoothStep(0f, 1f, flight));
        Vector2 d = (Vector2.Zero - Origin).Normalized();
        Vector2 n = new(-d.Y, d.X);
        DrawLine(Origin, tip, new Color("AAB7C8", .9f * fade), 3f, true);
        DrawLine(tip - d * 22f, tip + d * 9f, new Color(Steel, fade), 7f, true);
        for (int i = -1; i <= 1; i++)
            DrawLine(tip, tip + d * 22f + n * (i * 16f), new Color(Steel, fade), 5f, true);
    }

    private void DrawWave(float t, float fade, Color color, int count, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * .5f) * 24f;
            float r = Mathf.Lerp(35f, radius + i * 12f, Mathf.Sqrt(t));
            DrawArc(new Vector2(offset, 0f), r, -2.25f, .65f, 28, new Color(color, fade * (1f - i * .16f)), 10f - i * 2f, true);
        }
    }

    private void DrawVortex(float t, float fade, Color color, bool spear)
    {
        float r = Mathf.Lerp(24f, spear ? 185f : 135f, Mathf.Sqrt(t));
        for (int i = 0; i < 3; i++)
            DrawArc(Vector2.Zero, r - i * 18f, -.3f + i * .55f + t * 2.2f, 4.7f + i * .55f + t * 2.2f, 42, new Color(color, fade * (1f - i * .2f)), 9f - i * 2f, true);
        if (spear) DrawLine(new Vector2(-r, 30f), new Vector2(r, -30f), new Color(Steel, fade), 7f, true);
    }

    private void DrawBurst(float t, float fade, Color outer, Color inner, int rays)
    {
        float radius = Mathf.Lerp(18f, 175f, Mathf.Sqrt(t));
        DrawCircle(Vector2.Zero, radius * .42f, new Color(inner, .32f * fade));
        DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 48, new Color(outer, .88f * fade), 10f, true);
        for (int i = 0; i < rays; i++)
        {
            Vector2 d = Vector2.FromAngle(i * Mathf.Tau / rays + .16f);
            DrawLine(d * radius * .38f, d * radius, new Color(i % 3 == 0 ? inner : outer, fade), i % 3 == 0 ? 8f : 5f, true);
        }
    }
}
