#nullable enable

using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Nodes.Vfx
{
    /// <summary>
    /// A restrained, persistent mark painted directly over a marked monster.
    /// The normal power row still owns the hover tip; this combat-space readout
    /// makes the target and current stack count legible at a glance.
    /// </summary>
    public sealed partial class IroneyeMarkStatusVfx : Node2D
    {
        private const string NodeName = "NightreignIroneyeMarkStatus";
        private static readonly Color Acid = new("#C8D94A");
        private static readonly Color Cyan = new("#4FC4C9");
        private static readonly Color Ink = new("#11180DE8");
        private static readonly Vector2 MarkerOffset = new(0f, -82f);

        private Creature _target = null!;
        private Label _amountLabel = null!;
        private float _time;
        private float _pulse;

        public static void Ensure(Creature target)
        {
            if (TestMode.IsOn || target == null || target.IsDead)
                return;

            // Keep the persistent readout in the creature scene.  The global
            // VFX container renders above pause/menu overlays, which made the
            // mark stay fully lit while the rest of combat was dimmed.
            Node? container = target.GetCreatureNode();
            if (container == null)
                return;

            IroneyeMarkStatusVfx? existing =
                container.GetNodeOrNull<IroneyeMarkStatusVfx>(NodeName);
            if (existing != null)
            {
                existing._pulse = 1f;
                return;
            }

            var effect = new IroneyeMarkStatusVfx
            {
                Name = NodeName,
                _target = target,
                ZIndex = 2,
            };
            container.AddChildSafely(effect);
        }

        public static void Pulse(Creature target)
        {
            Node? container = target?.GetCreatureNode();
            IroneyeMarkStatusVfx? effect =
                container?.GetNodeOrNull<IroneyeMarkStatusVfx>(NodeName);
            if (effect != null)
                effect._pulse = 1f;
        }

        public static void Remove(Creature target)
        {
            Node? container = target?.GetCreatureNode();
            container?.GetNodeOrNull<IroneyeMarkStatusVfx>(NodeName)?.QueueFree();
        }

        public override void _Ready()
        {
            _amountLabel = new Label
            {
                Position = new Vector2(31f, 17f),
                Size = new Vector2(58f, 45f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _amountLabel.AddThemeFontSizeOverride("font_size", 28);
            _amountLabel.AddThemeConstantOverride("outline_size", 9);
            _amountLabel.AddThemeColorOverride("font_color", Acid);
            _amountLabel.AddThemeColorOverride("font_outline_color", Ink);
            AddChild(_amountLabel);

            SetProcess(true);
            Refresh();
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            _pulse = Math.Max(0f, _pulse - (float)delta * 2.8f);

            if (!Refresh())
            {
                QueueFree();
                return;
            }

            QueueRedraw();
        }

        private bool Refresh()
        {
            if (_target == null || _target.IsDead)
                return false;

            MarkPower? mark = _target.GetPower<MarkPower>();
            if (mark == null || mark.Amount <= 0)
                return false;

            var creatureNode = _target.GetCreatureNode();
            if (creatureNode == null)
                return false;

            GlobalPosition = creatureNode.VfxSpawnPosition + MarkerOffset;
            if (_amountLabel != null)
                _amountLabel.Text = mark.Amount.ToString();
            return true;
        }

        public override void _Draw()
        {
            float breathe = 0.5f + 0.5f * Mathf.Sin(_time * 2.4f);
            float pulseScale = 1f + _pulse * 0.24f;
            float radius = (43f + breathe * 2f) * pulseScale;
            Color acid = WithAlpha(Acid, 0.30f + _pulse * 0.34f);
            Color cyan = WithAlpha(Cyan, 0.20f + _pulse * 0.25f);

            DrawCircle(Vector2.Zero, radius + 8f, WithAlpha(Ink, 0.16f));
            DrawArc(Vector2.Zero, radius, -2.85f, -0.22f, 28, acid, 4f, true);
            DrawArc(Vector2.Zero, radius, 0.28f, 2.58f, 28, cyan, 3f, true);
            DrawArc(Vector2.Zero, radius * 0.68f, 1.7f, 5.55f, 24,
                WithAlpha(Acid, 0.22f + _pulse * 0.28f), 3f, true);

            // A pale hooked blade and puncture star echo Ironeye's mark glyph
            // without obscuring the monster art underneath.
            DrawArc(new Vector2(-5f, 4f), radius * 0.76f, -2.55f, 0.72f, 24,
                WithAlpha(Acid, 0.24f + _pulse * 0.35f), 6f, true);
            DrawLine(new Vector2(-25f, -24f), new Vector2(25f, 24f),
                WithAlpha(Cyan, 0.22f + _pulse * 0.40f), 4f, true);
            DrawLine(new Vector2(-18f, 26f), new Vector2(22f, -23f),
                WithAlpha(Acid, 0.28f + _pulse * 0.42f), 4f, true);
            DrawCircle(Vector2.Zero, 4f + _pulse * 4f,
                WithAlpha(Colors.White, 0.34f + _pulse * 0.45f));
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.R, color.G, color.B, Math.Clamp(alpha, 0f, 1f));
    }
}
