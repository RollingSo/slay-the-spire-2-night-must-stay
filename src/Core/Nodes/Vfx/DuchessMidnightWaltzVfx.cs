using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>
/// Gives Midnight Waltz the same staged anticipation and synchronized impact
/// cadence as Grand Finale, then layers Duchess-owned moonlit stage graphics
/// over the original production VFX.
/// </summary>
public partial class DuchessMidnightWaltzVfx : Node2D
{
    private enum Phase { Prelude, Impact }

    private static readonly Color Night = new("071226");
    private static readonly Color Moon = new("DDEBFF");
    private static readonly Color Blue = new("75AFFF");
    private static readonly Color Gold = new("FFD98A");

    private Phase _phase;
    private float _age;
    private Vector2 _focus;

    public static async Task PlayPrelude(Creature owner)
    {
        if (TestMode.IsOn)
            return;

        Control container = NCombatRoom.Instance?.CombatVfxContainer;
        NGrandFinaleVfx original = NGrandFinaleVfx.Create(owner);
        if (container != null && original != null)
            container.AddChildSafely(original);

        if (container != null && owner?.GetCreatureNode() is { } ownerNode)
        {
            var accent = new DuchessMidnightWaltzVfx
            {
                Name = "DuchessMidnightWaltzPrelude",
                _phase = Phase.Prelude,
                _focus = ownerNode.VfxSpawnPosition,
                ZIndex = 26,
            };
            container.AddChildSafely(accent);
        }

        // Grand Finale deliberately holds damage until its spotlight, wind-up,
        // slash and hit cue have completed. Keep the exact production timing.
        await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration, false);
    }

    public static Node2D CreateImpact(Creature target)
    {
        if (TestMode.IsOn)
            return null;

        NGrandFinaleImpactVfx original = NGrandFinaleImpactVfx.Create(target);
        if (target?.GetCreatureNode() is not { } targetNode)
            return original;

        var root = new DuchessMidnightWaltzVfx
        {
            Name = "DuchessMidnightWaltzImpact",
            _phase = Phase.Impact,
            _focus = targetNode.VfxSpawnPosition,
            ZIndex = 27,
        };
        if (original != null)
            root.AddChild(original);
        return root;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float lifetime = _phase == Phase.Prelude
            ? NGrandFinaleVfx.totalAnticipationDuration + 0.12f
            : 0.7f;
        if (_age >= lifetime)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_phase == Phase.Prelude)
            DrawPrelude();
        else
            DrawImpact();
    }

    private void DrawPrelude()
    {
        float duration = NGrandFinaleVfx.totalAnticipationDuration;
        float t = Mathf.Clamp(_age / duration, 0f, 1f);
        float swell = Mathf.SmoothStep(0f, 1f, t);
        Vector2 size = GetViewportRect().Size;

        DrawRect(new Rect2(Vector2.Zero, size), new Color(Night, 0.12f + swell * 0.34f));

        float ringRadius = Mathf.Lerp(185f, 78f, swell);
        DrawArc(_focus, ringRadius, 0f, Mathf.Tau, 96,
            new Color(Blue, 0.2f + 0.65f * swell), 4f + 4f * swell, true);
        DrawArc(_focus, ringRadius + 14f, -2.7f, 1.15f, 58,
            new Color(Moon, 0.15f + 0.72f * swell), 7f, true);

        for (int i = 0; i < 12; i++)
        {
            float angle = -Mathf.Pi / 2f + i * Mathf.Tau / 12f;
            Vector2 direction = Vector2.FromAngle(angle);
            float tick = i == 0 ? 22f : 13f;
            DrawLine(_focus + direction * (ringRadius - tick),
                _focus + direction * (ringRadius + 4f),
                new Color(i == 0 ? Gold : Moon, 0.25f + 0.7f * swell),
                i == 0 ? 5f : 3f, true);
        }

        float sweep = Mathf.Lerp(-size.X * 0.2f, size.X * 1.1f, t);
        for (int i = 0; i < 3; i++)
        {
            float y = _focus.Y - 105f + i * 42f;
            DrawLine(new Vector2(sweep - 250f - i * 55f, y + 70f),
                new Vector2(sweep + 95f, y - 70f),
                new Color(i == 1 ? Moon : Blue, 0.12f + 0.36f * swell),
                9f - i * 2f, true);
        }
    }

    private void DrawImpact()
    {
        float t = Mathf.Clamp(_age / 0.7f, 0f, 1f);
        float fade = 1f - t;
        float reach = Mathf.Lerp(55f, 205f, Mathf.Sqrt(t));

        DrawCircle(_focus, Mathf.Lerp(12f, 72f, t),
            new Color(Moon, 0.28f * fade));
        DrawArc(_focus, Mathf.Lerp(28f, 145f, t), -2.75f, 1.25f, 48,
            new Color(Blue, 0.9f * fade), 9f * fade + 2f, true);

        for (int i = 0; i < 4; i++)
        {
            float angle = -0.85f + i * 0.55f;
            Vector2 direction = Vector2.FromAngle(angle);
            Vector2 normal = new(-direction.Y, direction.X);
            Vector2 center = _focus + normal * ((i - 1.5f) * 18f);
            DrawLine(center - direction * reach, center + direction * reach,
                new Color(i % 2 == 0 ? Moon : Gold, fade), 8f - i, true);
            DrawLine(center - direction * reach, center + direction * reach,
                new Color(Blue, 0.7f * fade), 2f, true);
        }

        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.Tau / 12f + 0.25f;
            Vector2 p = _focus + Vector2.FromAngle(angle) * Mathf.Lerp(34f, 176f, t);
            DrawCircle(p, 2.5f + (i % 3), new Color(i % 3 == 0 ? Gold : Moon, fade));
        }
    }
}
