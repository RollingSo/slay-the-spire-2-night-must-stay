using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Nodes
{
    /// <summary>
    /// A persistent, character-anchored Distance readout. It follows the same
    /// visual anchor and local/remote scaling rules as the Defect's orb manager,
    /// while remaining independent from orb slots and orb gameplay.
    /// </summary>
    public sealed partial class IroneyeDistanceIndicator : Control
    {
        private static readonly Vector2 ZeroDistanceOffset = new(0f, -420f);
        private static readonly Vector2 PositiveDistanceOffset = new(-235f, -225f);
        private static readonly Vector2 NegativeDistanceOffset = new(235f, -225f);

        private NCreature _creature = null!;
        private Label _amountLabel = null!;
        private int _displayedAmount = int.MinValue;

        public static IroneyeDistanceIndicator Create(NCreature creature)
        {
            var indicator = new IroneyeDistanceIndicator
            {
                Name = "IroneyeDistanceIndicator",
                _creature = creature,
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 0,
            };
            return indicator;
        }

        public override void _Ready()
        {
            // NOrbManager is a normal top-left anchored child of NCreature.
            // Keep this readout in the same local coordinate space so it follows
            // the creature instead of behaving like screen-space UI.
            SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            CustomMinimumSize = new Vector2(76f, 76f);
            Size = CustomMinimumSize;
            PivotOffset = Size * 0.5f;

            var icon = new TextureRect
            {
                Name = "DistanceIcon",
                Position = new Vector2(-31f, -31f),
                Size = new Vector2(62f, 62f),
                Texture = ModelDb.Power<DistancePower>().BigIcon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Modulate = new Color("E4F26CD9"),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(icon);

            _amountLabel = new Label
            {
                Name = "Amount",
                Position = new Vector2(-54f, -26f),
                Size = new Vector2(108f, 52f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _amountLabel.AddThemeFontSizeOverride("font_size", 32);
            _amountLabel.AddThemeConstantOverride("outline_size", 10);
            _amountLabel.AddThemeColorOverride("font_outline_color", new Color("151B10F2"));
            AddChild(_amountLabel);

            UpdateAmount(force: true);
            UpdateTransformFromCreature();
        }

        public override void _Process(double delta)
        {
            if (!GodotObject.IsInstanceValid(_creature))
            {
                QueueFree();
                return;
            }

            UpdateAmount(force: false);
            UpdateTransformFromCreature();
        }

        private void UpdateTransformFromCreature()
        {
            float visualScale = _creature.Visuals.Scale.X;
            Scale = visualScale > 1f
                ? Vector2.One
                : _creature.Visuals.Scale.Lerp(Vector2.One, 0.5f);

            Vector2 offset = _displayedAmount > 0
                ? PositiveDistanceOffset
                : _displayedAmount < 0
                    ? NegativeDistanceOffset
                    : ZeroDistanceOffset;
            Position = offset * Mathf.Min(visualScale, 1.25f);
            if (!LocalContext.IsMe(_creature.Entity))
                Position += Vector2.Up * 50f;
        }

        private void UpdateAmount(bool force)
        {
            int amount = _creature.Entity.GetPower<DistancePower>()?.Amount ?? 0;
            if (!force && amount == _displayedAmount)
                return;

            _displayedAmount = amount;
            _amountLabel.Text = amount > 0 ? $"+{amount}" : amount.ToString();
            _amountLabel.AddThemeColorOverride(
                "font_color",
                amount > 0
                    ? new Color("DDF568")
                    : amount < 0
                        ? new Color("F0A45C")
                        : new Color("FFF5D8"));
        }
    }
}
