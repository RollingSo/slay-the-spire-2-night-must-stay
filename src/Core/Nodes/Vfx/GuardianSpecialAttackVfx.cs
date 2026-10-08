#nullable enable
using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Four independent attack silhouettes; deterministic and presentation-only.</summary>
public partial class GuardianSpecialAttackVfx : CombatVfxCanvas
{
    public enum Kind { Topple, Heavenfall, PhantomCoStrike, PhantomSpear }
    public Kind AttackKind { get; set; }
    public bool EchoOnly { get; set; }
    public Action? OnStart { get; set; }
    public float Duration => AttackKind == Kind.Heavenfall ? 1.8f : .65f;
    private float _age;
    private bool _manual;
    private readonly Vector2[] _curve = new Vector2[25];
    private readonly Color[] _colors = new Color[25];
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
        float fade = Ease(0, .06f, t) * (1 - Ease(.6f, 1, t));
        if (fade < .001f) return;
        Color air = Fade(new Color("B9DEE0"), fade * .65f);
        Color white = Fade(new Color("EDF7F1"), fade);
        Color ghost = Fade(new Color("8DCADB"), fade * .65f);
        if (AttackKind == Kind.Topple)
        {
            // Upward, curling air lanes, no weapon or water-like filled body.
            for (int lane = 0; lane < 4; lane++)
            {
                for (int i = 0; i < _curve.Length; i++)
                {
                    float u = i / (float)(_curve.Length - 1);
                    _curve[i] = new Vector2((lane - 1.5f) * 38 + MathF.Sin(u * 3 + t * 5 + lane) * (12 + u * 25),
                        115 - u * 185 - t * 140);
                }
                Ribbon(air, 12); Ribbon(white, 2);
            }
            Splinters(t, new Vector2(0, 55 - t * 180), air, 6, 55, -MathF.PI * .5f);
            return;
        }
        if (AttackKind == Kind.Heavenfall)
        {
            Color gold = Fade(new Color("FFD478"), (1 - Ease(.25f, 1, t)) * .9f);
            Color cyan = Fade(new Color("4AC6EF"), (1 - Ease(.2f, .85f, t)) * .8f);
            // Arrival is immediate: this node is spawned only after the 1.625s prelude.
            float y = -260 + Ease(0, .1f, t) * 305;
            if (t < .22f)
            {
                Shard(new Vector2(0, y - 100), 175, 45, MathF.PI * .5f, cyan);
                Shard(new Vector2(0, y - 45), 100, 13, MathF.PI * .5f, Fade(new Color("FFFFFF"), 1 - t * 3));
                for (int lane = -3; lane <= 3; lane++)
                    DrawLine(new Vector2(lane * 50, y - 320), new Vector2(lane * 15, y - 20), cyan, 5, true);
            }
            if (t >= .08f)
            {
                float u = (t - .08f) / .92f;
                for (int ring = 0; ring < 3; ring++)
                {
                    float radius = 70 + Ease(0, .65f, u) * (340 - ring * 65) + u * 35;
                    for (int i = 0; i < _curve.Length; i++)
                    {
                        float a = i * MathF.Tau / (_curve.Length - 1);
                        _curve[i] = new Vector2(MathF.Cos(a) * radius, 65 + MathF.Sin(a) * radius * .23f);
                    }
                    Ribbon(gold, 24 - ring * 5); Ribbon(Fade(new Color("FFF4CE"), gold.A), 4);
                }
                Splinters(u, new Vector2(0, 50), gold, 16, 275);
                for (int feather = 0; feather < 8; feather++)
                {
                    float a = feather * MathF.Tau / 8;
                    Vector2 p = new(MathF.Cos(a) * (45 + u * 260), 40 - MathF.Sin(a) * 60 - u * 100);
                    Shard(p, 22 * (1 - u * .5f), 5, a + u * 2, cyan);
                }
            }
            return;
        }
        if (AttackKind == Kind.PhantomSpear)
        {
            Vector2 tip = new(-230 + Ease(0, .5f, t) * 295 + t * 20, -10);
            Lance(tip + new Vector2(-26, -13), Fade(ghost, ghost.A * .35f));
            Lance(tip, ghost); Lance(tip, Fade(white, white.A * .7f), 2);
            if (t > .28f) Splinters((t - .28f) / .72f, Vector2.Zero, white, 6, 70);
            return;
        }
        // Card portrait: physical Guardian and blue echo thrust together, same direction.
        for (int figure = EchoOnly ? 1 : 0; figure < 2; figure++)
        {
            Vector2 origin = new(-150 + Ease(0, .65f, t) * 220 + t * 20, figure == 0 ? 20 : -65);
            Color ink = figure == 0 ? Fade(new Color("CDD7DB"), fade * .8f) : Fade(new Color("60CFFF"), fade * .7f);
            GuardianEcho(origin, ink);
            Lance(origin + new Vector2(100, 15), ink);
            DrawLine(origin - new Vector2(155, -40), origin + new Vector2(25, 45), Fade(ink, ink.A * .35f), 13, true);
        }
        if (t > .2f) Splinters((t - .2f) / .8f, Vector2.Zero, ghost, 7, 85);
    }
    private void GuardianEcho(Vector2 p, Color ink)
    {
        // Eagle beak, crouched torso, pointed skirt and a grouped trailing wing.
        Poly(ink, p + new Vector2(-16, -42), p + new Vector2(4, -62), p + new Vector2(25, -60),
            p + new Vector2(45, -43), p + new Vector2(22, -38), p + new Vector2(9, -25));
        Poly(ink, p + new Vector2(-20, -28), p + new Vector2(20, -24), p + new Vector2(40, 16),
            p + new Vector2(15, 35), p + new Vector2(-20, 21));
        Poly(ink, p + new Vector2(-15, -29), p + new Vector2(-105, -42), p + new Vector2(-71, -15),
            p + new Vector2(-100, -7), p + new Vector2(-52, 8), p + new Vector2(-74, 26), p + new Vector2(-15, 18));
        Poly(ink, p + new Vector2(-20, 16), p + new Vector2(17, 22), p + new Vector2(6, 58),
            p + new Vector2(-13, 38), p + new Vector2(-39, 64));
    }
    private void Ribbon(Color color, float width)
    {
        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = Fade(color, color.A * MathF.Sin(i * MathF.PI / (_colors.Length - 1)));
        DrawPolylineColors(_curve, _colors, width, true);
    }
    private void Lance(Vector2 tip, Color color, float width = 5)
    {
        DrawLine(tip - new Vector2(210, 0), tip - new Vector2(32, 0), color, width, true);
        Poly(color, tip, tip + new Vector2(-45, -10), tip + new Vector2(-33, 0), tip + new Vector2(-45, 10));
    }
}
