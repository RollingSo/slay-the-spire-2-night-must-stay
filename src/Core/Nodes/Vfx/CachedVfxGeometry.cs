using System;
using Godot;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Reusable geometry; no change to ribbon samples, triangle order or blending.</summary>
internal sealed class CachedVfxStroke
{
    private readonly Vector2[] _outline;
    private readonly int[] _indices;
    private readonly Color[] _color = new Color[1];

    internal CachedVfxStroke(int pointCount)
    {
        _outline = new Vector2[pointCount * 2];
        _indices = new int[(pointCount - 1) * 6];
    }

    internal void Draw(Rid canvas, Vector2[] points, float width, Color color)
    {
        if (color.A <= .001f || width <= .01f) return;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 tangent = points[Math.Min(points.Length - 1, i + 1)] - points[Math.Max(0, i - 1)];
            float u = i / (float)(points.Length - 1);
            float w = width * .5f * (.02f + .98f * MathF.Sin(u * MathF.PI));
            Vector2 normal = tangent.Normalized().Orthogonal() * w;
            _outline[i] = points[i] + normal;
            _outline[_outline.Length - 1 - i] = points[i] - normal;
        }
        int indexCount = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            int opposite = _outline.Length - 1 - i;
            AddTriangle(i, i + 1, opposite - 1);
            AddTriangle(i, opposite - 1, opposite);
        }
        if (indexCount == 0) return;
        _color[0] = color;
        // The server copies these buffers synchronously. Keep the original alpha
        // overlap order, but avoid one C#/native call and triangulation per triangle.
        RenderingServer.CanvasItemAddTriangleArray(canvas, _indices, _outline, _color,
            count: indexCount / 3);

        void AddTriangle(int a, int b, int c)
        {
            float area = _outline[a].Cross(_outline[b]) + _outline[b].Cross(_outline[c]) + _outline[c].Cross(_outline[a]);
            if (Math.Abs(area) <= .01f) return;
            _indices[indexCount++] = a;
            _indices[indexCount++] = b;
            _indices[indexCount++] = c;
        }
    }
}

/// <summary>Fixed arc angles are evaluated once, not on every redraw.</summary>
internal sealed class CachedVfxArc
{
    private readonly Vector2[] _directions;
    private readonly Vector2[] _points;

    internal CachedVfxArc(Vector2[] directions)
    {
        _directions = directions;
        _points = new Vector2[directions.Length];
    }

    internal static Vector2[] Directions(float start, float end, int count)
    {
        var directions = new Vector2[count];
        // Match Godot 4.5.1 CanvasItem::draw_arc, including float operation
        // order. Rearranging the fraction changes antialiased edge pixels.
        float delta = Math.Clamp(end - start, -Mathf.Tau, Mathf.Tau);
        for (int i = 0; i < count; i++)
            directions[i] = Vector2.FromAngle((i / (float)(count - 1)) * delta + start);
        return directions;
    }

    internal void Draw(Node2D canvas, Vector2 center, float radius, Color color, float width)
    {
        for (int i = 0; i < _points.Length; i++)
            _points[i] = center + _directions[i] * radius;
        canvas.DrawPolyline(_points, color, width, true);
    }
}
