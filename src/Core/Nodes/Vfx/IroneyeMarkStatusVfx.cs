#nullable enable

using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Nodes.Vfx
{
    /// <summary>
    /// A restrained, persistent mark painted directly over a marked monster.
    /// The normal power row still owns the hover tip; this combat-space readout
    /// makes the target and current stack count legible at a glance.
    /// </summary>
    public sealed partial class IroneyeMarkStatusVfx : IroneyeMarkGlyph
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
                _pulse = 1f,
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
            base._Ready();
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

            NightMustStayMarkPower? mark = _target.GetPower<NightMustStayMarkPower>();
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

        public override void _Draw() => DrawMark(_time, _pulse);
    }
}
