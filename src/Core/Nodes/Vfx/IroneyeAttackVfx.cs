#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Compact cuts, directed arrows and a distinct weak-point fracture. No gameplay RNG.</summary>
public partial class IroneyeAttackVfx : Node2D
{
    public enum Kind { Knife, Shot, MarkTrigger, PoisonKnife, PoisonShot, PoisonBurst }
    public Kind AttackKind { get; set; }
    // Shot nodes are anchored at the destination; this is the source in local coordinates.
    public Vector2 ShotOrigin { get; set; } = new(-360, 0);
    public Action? OnStart { get; set; }
    public float Duration => AttackKind switch { Kind.Knife or Kind.PoisonKnife => 0.28f, Kind.Shot or Kind.PoisonShot => 0.36f, Kind.PoisonBurst => 0.56f, _ => 0.44f };
    public const float FlightSeconds = 0.09f;
    private float _age;
    private static readonly Color Ink = new("#17222A");
    private static readonly Color Bone = new("#D6D3C3");
    private static readonly Color White = new("#F5F4DA");
    private static readonly Color Acid = new("#C8D94A");
    private static readonly Color Cyan = new("#4FC4C9");
    private static readonly Color Venom = new("#81D94A");
    private static readonly Color DeepVenom = new("#365B32");

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

    public override void _Draw() => DrawFrame(Math.Clamp(_age / Duration, 0, 1));

    // Shared with the isolated GPU review scene; all points are local to the target.
    public void DrawFrame(float t)
    {
        t = Math.Clamp(t, 0, 1);
        float alpha = 1 - Smooth(0.42f, 1, t);
        if (alpha <= 0) return;
        switch (AttackKind)
        {
            case Kind.Knife: DrawKnife(t, alpha); break;
            case Kind.Shot: DrawShot(t, alpha); break;
            case Kind.MarkTrigger: DrawMark(t, alpha); break;
            case Kind.PoisonKnife: DrawKnife(t, alpha); DrawVenomSplash(t, alpha, 0.6f); break;
            case Kind.PoisonShot: DrawShot(t, alpha); break;
            case Kind.PoisonBurst: DrawPoisonBurst(t, alpha); break;
        }
    }

    private void DrawKnife(float t, float alpha)
    {
        // A close-range cut across the target, substantially smaller than Guardian's halberd.
        float cut = Smooth(0, 0.26f, t);
        float retract = Smooth(0.35f, 1, t);
        Vector2 center = new(-22 + cut * 36, 12 - cut * 20);
        float length = (72 + cut * 22) * (1 - retract * 0.75f);
        Cut(center, length + 5, 13, -0.66f, Tint(Ink, alpha));
        bool poisoned = AttackKind == Kind.PoisonKnife;
        Cut(center, length, 8, -0.66f, Tint(poisoned ? Venom : Bone, alpha));
        Cut(center + new Vector2(0, -3), length * 0.83f, 2.5f, -0.66f, Tint(White, alpha));
        Cut(center + new Vector2(-10, 13), length * 0.7f, 3, -0.66f, Tint(Acid, alpha * 0.8f));
        float spark = 1 - Smooth(0.1f, 0.55f, t);
        Cut(Vector2.Zero, 17 * spark, 5 * spark, -0.66f, Tint(White, alpha));
        for (int i = 0; i < 3; i++)
        {
            Vector2 d = Vector2.FromAngle(-1.35f + i * 0.65f);
            Cut(d * (22 + 53 * t), 7 * (1 - t), 2, d.Angle(), Tint(Acid, alpha * spark));
        }
    }

    public Vector2 ArrowTipAt(float normalizedTime)
    {
        float flight = Math.Clamp(normalizedTime * Duration / FlightSeconds, 0, 1);
        return ShotOrigin.Lerp(Vector2.Zero, 1 - (1 - flight) * (1 - flight));
    }

    private void DrawShot(float t, float alpha)
    {
        Vector2 direction = ShotOrigin.LengthSquared() > 0.001f ? -ShotOrigin.Normalized() : Vector2.Right;
        Vector2 normal = direction.Orthogonal();
        float seconds = t * Duration;
        if (seconds < FlightSeconds + 0.045f)
        {
            Vector2 tip = ArrowTipAt(t);
            float arrowAlpha = alpha * (1 - Smooth(FlightSeconds, FlightSeconds + 0.045f, seconds));
            // Only a short tapered wake follows the arrow; never draw a persistent full-screen beam.
            Vector2 tail = tip - direction * 58;
            Triangle(tail - direction * 90, tail + normal * 7, tip - direction * 12, Tint(Ink, arrowAlpha));
            bool poisoned = AttackKind == Kind.PoisonShot;
            Triangle(tail - direction * 65, tail + normal * (poisoned ? 9 : 3), tip - direction * 12, Tint(poisoned ? Venom : Cyan, arrowAlpha * 0.8f));
            DrawLine(tail, tip - direction * 11, Tint(Ink, arrowAlpha), 6, true);
            DrawLine(tail, tip - direction * 11, Tint(Bone, arrowAlpha), 2.5f, true);
            Triangle(tip, tip - direction * 18 + normal * 6, tip - direction * 14 - normal * 4, Tint(poisoned ? Venom : White, arrowAlpha));
            Triangle(tail + direction * 13, tail - direction * 10 + normal * 8, tail, Tint(Bone, arrowAlpha));
            Triangle(tail + direction * 13, tail - direction * 10 - normal * 8, tail, Tint(Bone, arrowAlpha));
        }
        if (seconds < FlightSeconds) return;
        float hit = Math.Clamp((seconds - FlightSeconds) / (Duration - FlightSeconds), 0, 1);
        if (AttackKind == Kind.PoisonShot) DrawVenomSplash(hit, alpha, 0.7f);
        float impact = alpha * (1 - Smooth(0.1f, 0.8f, hit));
        Cut(Vector2.Zero, 34 * (1 - hit) + 5, 6 * (1 - hit), direction.Angle(), Tint(White, impact));
        Cut(Vector2.Zero, 16 * (1 - hit), 3, direction.Angle() + 1.5f, Tint(Cyan, impact));
        for (int i = 0; i < 3; i++)
        {
            Vector2 d = direction.Rotated(-0.8f + i * 0.8f);
            Cut(d * (16 + hit * 49), 8 * (1 - hit), 2, d.Angle(), Tint(Bone, impact));
        }
    }

    private void DrawMark(float t, float alpha)
    {
        float close = Smooth(0, 0.16f, t);
        float fracture = Smooth(0.17f, 0.65f, t);
        // Four inward points snap onto one X, then split. No concentric magic circles.
        for (int i = 0; i < 4; i++)
        {
            float a = -MathF.PI / 4 + i * MathF.PI / 2;
            Vector2 d = Vector2.FromAngle(a), n = d.Orthogonal();
            float radius = Mathf.Lerp(68, 43, close) + 54 * fracture;
            Vector2 p = d * radius;
            float weight = 1 - Smooth(0.38f, 0.95f, t);
            Triangle(p - d * 18, p + d * 14 + n * 9, p + d * 14 - n * 9, Tint(Ink, alpha * weight));
            Triangle(p - d * 14, p + d * 9 + n * 5, p + d * 9 - n * 5, Tint(i % 2 == 0 ? Acid : Cyan, alpha * weight));
        }
        float core = close * (1 - Smooth(0.3f, 0.74f, t));
        Cut(Vector2.Zero, 52 * core, 12 * core, -MathF.PI / 4, Tint(Ink, alpha));
        Cut(Vector2.Zero, 49 * core, 8 * core, -MathF.PI / 4, Tint(Acid, alpha));
        Cut(Vector2.Zero, 46 * core, 8 * core, MathF.PI / 4, Tint(Cyan, alpha));
        Cut(Vector2.Zero, 28 * core, 3 * core, -MathF.PI / 4, Tint(White, alpha));
        for (int i = 0; i < 4; i++)
        {
            Vector2 d = Vector2.FromAngle(i * MathF.PI / 2);
            Cut(d * (12 + fracture * 61), 9 * (1 - fracture), 2, d.Angle(), Tint(Bone, alpha * core));
        }
    }

    private void DrawVenomSplash(float t, float alpha, float scale)
    {
        // Five designed drops, no random particles or persistent fog overlay.
        for (int i = 0; i < 5; i++)
        {
            Vector2 d = Vector2.FromAngle(-2.8f + i * 1.23f);
            Vector2 p = d * (22 + 69 * t) * scale + new Vector2(0, 29 * t * t);
            float radius = (6 - 4 * t) * scale;
            DrawCircle(p, radius + 2, Tint(Ink, alpha));
            DrawCircle(p, radius, Tint(i % 2 == 0 ? Venom : Acid, alpha));
        }
    }

    private void DrawPoisonBurst(float t, float alpha)
    {
        float expand = Smooth(0, 0.65f, t);
        // A compressed toxic core ruptures into six broad lobes. The silhouette
        // is rounded and radial, unlike the angular four-point Mark fracture.
        float radius = 12 + 57 * expand;
        for (int i = 0; i < 6; i++)
        {
            Vector2 d = Vector2.FromAngle(i * MathF.Tau / 6 - 0.3f);
            Vector2 p = d * radius;
            float size = (11 + 13 * MathF.Sin(MathF.PI * t)) * (1 - 0.55f * t);
            DrawCircle(p, size + 3, Tint(Ink, alpha));
            DrawCircle(p, size, Tint(DeepVenom, alpha));
            DrawCircle(p - d * 5, size * 0.65f, Tint(Venom, alpha));
        }
        float core = 1 - Smooth(0.12f, 0.65f, t);
        Cut(Vector2.Zero, 39 * core, 27 * core, 0.25f, Tint(Ink, alpha));
        Cut(Vector2.Zero, 34 * core, 22 * core, 0.25f, Tint(Acid, alpha));
        DrawVenomSplash(t, alpha, 1.2f);
    }

    private void Cut(Vector2 center, float length, float width, float angle, Color color)
    {
        if (length < 0.01f || width < 0.01f || color.A < 0.001f) return;
        Vector2 d = Vector2.FromAngle(angle), n = d.Orthogonal();
        DrawColoredPolygon(new[] { center - d * length, center - d * length * 0.14f + n * width,
            center + d * length, center + d * length * 0.12f - n * width }, color);
    }

    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (color.A > 0.001f) DrawColoredPolygon(new[] { a, b, c }, color);
    }

    private static Color Tint(Color color, float alpha) => new(color.R, color.G, color.B, Math.Clamp(alpha, 0, 1));
    private static float Smooth(float a, float b, float t)
    {
        t = Math.Clamp((t - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
