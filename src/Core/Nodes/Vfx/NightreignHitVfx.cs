#nullable enable

using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.TestSupport;

namespace sts2mod.Core.Nodes.Vfx
{
    /// <summary>
    /// Lightweight, project-owned hit effects for Nightreign characters.
    /// The shapes are drawn at runtime so none of the effects depend on an
    /// original-character texture, atlas, or scene.
    /// </summary>
    public partial class NightreignHitVfx : Node2D
    {
        private enum Style
        {
            IroneyeShot,
            IroneyeMarkTrigger,
            IroneyeKnife,
            GuardianWhirlwind,
            GuardianCounter,
            GuardianShieldPoke,
        }

        private static readonly Color IroneyeAcid = new("#C8D94A");
        private static readonly Color IroneyeCyan = new("#4FC4C9");
        private static readonly Color IroneyeSteel = new("#D6D3C3");
        private static readonly Color GuardianWind = new("#77D9E8");
        private static readonly Color GuardianSteel = new("#D9E4EA");
        private static readonly Color GuardianCounterOchre = new("#C69A55");
        private static readonly Color GuardianShieldBlue = new("#4386A8");

        private Style _style;
        private float _age;
        private float _duration = 0.48f;
        private Vector2 _travelVector;

        public static Node2D? CreateIroneyeShot(Creature attacker, Creature target)
        {
            if (TestMode.IsOn
                || attacker == null
                || target == null
                || target.IsDead)
            {
                return null;
            }

            var attackerNode = attacker.GetCreatureNode();
            var targetNode = target.GetCreatureNode();
            if (attackerNode == null || targetNode == null)
                return null;

            var effect = new NightreignHitVfx
            {
                _style = Style.IroneyeShot,
                _travelVector = targetNode.VfxSpawnPosition - attackerNode.VfxSpawnPosition,
                GlobalPosition = attackerNode.VfxSpawnPosition,
                ZIndex = 20,
            };
            return effect;
        }

        public static Node2D? CreateIroneyeMarkTrigger(Creature target) =>
            Create(target, Style.IroneyeMarkTrigger);

        public static Node2D? CreateIroneyeKnife(Creature target) =>
            Create(target, Style.IroneyeKnife);

        public static Node2D? CreateGuardianWhirlwind(Creature target) =>
            Create(target, Style.GuardianWhirlwind);

        public static Node2D? CreateGuardianCounter(Creature target) =>
            Create(target, Style.GuardianCounter);

        public static Node2D? CreateGuardianShieldPoke(Creature target) =>
            Create(target, Style.GuardianShieldPoke);

        public static void PlayIroneyeKnife(Creature target) =>
            Play(target, Style.IroneyeKnife);

        public static void PlayIroneyeMarkTrigger(Creature target) =>
            Play(target, Style.IroneyeMarkTrigger);

        public static void PlayGuardianWhirlwind(Creature target) =>
            Play(target, Style.GuardianWhirlwind);

        public static void PlayGuardianCounter(Creature target) =>
            Play(target, Style.GuardianCounter);

        private static NightreignHitVfx? Create(Creature target, Style style)
        {
            if (TestMode.IsOn || target == null || target.IsDead)
                return null;

            var creatureNode = target.GetCreatureNode();
            if (creatureNode == null)
                return null;

            var effect = new NightreignHitVfx
            {
                _style = style,
                _duration = style == Style.IroneyeMarkTrigger ? 0.72f : 0.48f,
                GlobalPosition = creatureNode.VfxSpawnPosition,
                ZIndex = style == Style.IroneyeMarkTrigger ? 60 : 20,
            };
            return effect;
        }

        private static void Play(Creature target, Style style)
        {
            NightreignHitVfx? effect = Create(target, style);
            if (effect != null)
                target.GetVfxContainer()?.AddChildSafely(effect);
        }

        public override void _Ready()
        {
            SetProcess(true);
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= _duration)
            {
                QueueFree();
                return;
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            float t = Math.Clamp(_age / _duration, 0f, 1f);
            float appear = SmoothStep(0f, 0.14f, t);
            float disappear = 1f - SmoothStep(0.62f, 1f, t);
            float alpha = appear * disappear;
            if (alpha <= 0.001f)
                return;

            switch (_style)
            {
                case Style.IroneyeShot:
                    DrawIroneyeShot(t, alpha);
                    break;
                case Style.IroneyeMarkTrigger:
                    DrawIroneyeMarkTrigger(t, alpha);
                    break;
                case Style.IroneyeKnife:
                    DrawIroneyeKnife(t, alpha);
                    break;
                case Style.GuardianWhirlwind:
                    DrawGuardianWhirlwind(t, alpha);
                    break;
                case Style.GuardianCounter:
                    DrawGuardianCounter(t, alpha);
                    break;
                case Style.GuardianShieldPoke:
                    DrawGuardianShieldPoke(t, alpha);
                    break;
            }
        }

        private void DrawIroneyeShot(float t, float alpha)
        {
            Vector2 direction = _travelVector.LengthSquared() > 0.001f
                ? _travelVector.Normalized()
                : Vector2.Right;
            Vector2 normal = new(-direction.Y, direction.X);
            float flight = EaseOutCubic(Math.Min(t * 1.75f, 1f));
            Vector2 arrowCenter = _travelVector * flight;
            Vector2 arrowTip = arrowCenter + direction * 31f;
            Vector2 arrowTail = arrowCenter - direction * 43f;
            Color glow = WithAlpha(IroneyeAcid, alpha * 0.28f);
            Color core = WithAlpha(IroneyeSteel, alpha);
            Color accent = WithAlpha(IroneyeAcid, alpha);

            // A complete projectile travels from the attacker center to the target,
            // matching the original hunter shiv VFX's start/end trajectory model.
            DrawLine(arrowTail - direction * 48f, arrowCenter, glow, 14f, true);
            DrawLine(arrowTail, arrowTip - direction * 7f, core, 5f, true);
            DrawLine(arrowTail + normal * 2f, arrowTip - direction * 8f + normal * 2f, accent, 2f, true);
            DrawColoredPolygon(
                new[]
                {
                    arrowTip,
                    arrowTip - direction * 24f + normal * 10f,
                    arrowTip - direction * 16f,
                    arrowTip - direction * 24f - normal * 10f,
                },
                accent);

            // Two broad feathers keep the silhouette readable at combat scale.
            DrawColoredPolygon(
                new[]
                {
                    arrowTail,
                    arrowTail - direction * 21f + normal * 11f,
                    arrowTail - direction * 7f + normal * 2f,
                },
                core);
            DrawColoredPolygon(
                new[]
                {
                    arrowTail,
                    arrowTail - direction * 21f - normal * 11f,
                    arrowTail - direction * 7f - normal * 2f,
                },
                core);

            float burst = SmoothStep(0.52f, 0.76f, t);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.Pi / 3f;
                Vector2 burstDirection = Vector2.FromAngle(angle);
                DrawLine(
                    _travelVector + burstDirection * (10f + burst * 7f),
                    _travelVector + burstDirection * (22f + burst * 25f),
                    WithAlpha(IroneyeCyan, alpha * (1f - burst * 0.35f)),
                    3f,
                    true);
            }
        }

        private void DrawIroneyeMarkTrigger(float t, float alpha)
        {
            float pulse = EaseOutCubic(Math.Min(t * 1.8f, 1f));
            float impact = 1f - SmoothStep(0.05f, 0.34f, t);
            float radius = Mathf.Lerp(24f, 112f, pulse);
            Color cyan = WithAlpha(IroneyeCyan, alpha * 0.9f);
            Color acid = WithAlpha(IroneyeAcid, alpha);

            // A bright impact core, two rapidly expanding shock rings and a
            // large crossed reticle make the extra hit unmistakable even when
            // several combat effects resolve at once.
            DrawCircle(Vector2.Zero, 20f + impact * 28f,
                WithAlpha(Colors.White, alpha * impact * 0.72f));
            DrawCircle(Vector2.Zero, 12f + impact * 17f,
                WithAlpha(IroneyeAcid, alpha * impact));
            DrawArc(Vector2.Zero, radius, 0.05f, 6.05f, 52, cyan, 9f, true);
            DrawArc(Vector2.Zero, radius * 0.72f, 0.55f, 5.72f, 44,
                WithAlpha(IroneyeAcid, alpha * 0.92f), 7f, true);
            DrawArc(new Vector2(8f, 2f), radius * 0.46f, 2.1f, 5.95f, 34,
                WithAlpha(Colors.White, alpha * 0.82f), 5f, true);
            DrawLine(new Vector2(-82f, -72f), new Vector2(78f, 70f), acid, 12f, true);
            DrawLine(new Vector2(-67f, 78f), new Vector2(74f, -70f), cyan, 10f, true);
            DrawLine(new Vector2(-88f, -76f), new Vector2(84f, 76f),
                WithAlpha(Colors.White, alpha * 0.58f), 4f, true);
            DrawCircle(Vector2.Zero, 8f + pulse * 7f, WithAlpha(IroneyeSteel, alpha));

            for (int i = 0; i < 10; i++)
            {
                float angle = -1.9f + i * Mathf.Tau / 10f;
                Vector2 direction = Vector2.FromAngle(angle);
                DrawLine(
                    direction * (radius + 5f),
                    direction * (radius + 31f + (i % 3) * 10f),
                    WithAlpha(i % 2 == 0 ? IroneyeAcid : IroneyeSteel,
                        alpha * 0.84f),
                    i % 2 == 0 ? 5f : 3f,
                    true);
            }
        }

        private void DrawIroneyeKnife(float t, float alpha)
        {
            float sweep = EaseOutCubic(Math.Min(t * 1.55f, 1f));
            float end = Mathf.Lerp(-1.7f, 1.05f, sweep);
            Color glow = WithAlpha(IroneyeAcid, alpha * 0.3f);
            Color steel = WithAlpha(IroneyeSteel, alpha);
            Color cyan = WithAlpha(IroneyeCyan, alpha * 0.9f);

            DrawArc(new Vector2(-4f, 5f), 76f, -2.25f, end, 34, glow, 19f, true);
            DrawArc(new Vector2(-4f, 5f), 76f, -2.25f, end, 34, steel, 7f, true);
            DrawArc(new Vector2(14f, -7f), 53f, -2.4f, end - 0.25f, 28, cyan, 4f, true);
            DrawLine(new Vector2(-50f, 45f), new Vector2(48f, -43f), WithAlpha(IroneyeAcid, alpha), 5f, true);
            DrawLine(new Vector2(-34f, 54f), new Vector2(57f, -28f), WithAlpha(IroneyeSteel, alpha * 0.8f), 2f, true);
        }

        private void DrawGuardianWhirlwind(float t, float alpha)
        {
            float spin = EaseOutCubic(Math.Min(t * 1.4f, 1f));
            float rotation = -0.8f + spin * 2.2f;
            Color wind = WithAlpha(GuardianWind, alpha);
            Color steel = WithAlpha(GuardianSteel, alpha * 0.9f);

            DrawSetTransform(Vector2.Zero, rotation, Vector2.One);
            DrawArc(Vector2.Zero, 78f, -2.75f, 0.95f, 38, WithAlpha(GuardianWind, alpha * 0.25f), 22f, true);
            DrawArc(Vector2.Zero, 78f, -2.75f, 0.95f, 38, wind, 7f, true);
            DrawArc(new Vector2(3f, 1f), 53f, -2.35f, 1.2f, 30, steel, 5f, true);
            DrawArc(new Vector2(-5f, 4f), 31f, -2.05f, 1.45f, 22, WithAlpha(GuardianWind, alpha * 0.75f), 3f, true);

            for (int i = 0; i < 5; i++)
            {
                float angle = -2.45f + i * 0.72f;
                Vector2 origin = Vector2.FromAngle(angle) * (46f + i * 8f);
                Vector2 tangent = Vector2.FromAngle(angle + Mathf.Pi * 0.5f);
                DrawColoredPolygon(
                    new[]
                    {
                        origin - tangent * 4f,
                        origin + tangent * (18f + i * 2f),
                        origin + Vector2.FromAngle(angle) * 8f,
                    },
                    WithAlpha(i % 2 == 0 ? GuardianSteel : GuardianWind, alpha * 0.82f));
            }

            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }

        private void DrawGuardianCounter(float t, float alpha)
        {
            float thrust = EaseOutCubic(Math.Min(t * 1.7f, 1f));
            float tipX = Mathf.Lerp(-105f, 88f, thrust);
            Color ochre = WithAlpha(GuardianCounterOchre, alpha);
            Color steel = WithAlpha(GuardianSteel, alpha);
            Color wind = WithAlpha(GuardianWind, alpha * 0.7f);

            DrawArc(new Vector2(-30f, 8f), 64f, 0.55f, 4.9f, 34, WithAlpha(GuardianCounterOchre, alpha * 0.28f), 16f, true);
            DrawArc(new Vector2(-30f, 8f), 64f, 0.55f, 4.9f, 34, ochre, 5f, true);
            DrawLine(new Vector2(-118f, 0f), new Vector2(tipX, 0f), WithAlpha(GuardianWind, alpha * 0.3f), 14f, true);
            DrawLine(new Vector2(-112f, 0f), new Vector2(tipX, 0f), steel, 5f, true);
            DrawColoredPolygon(
                new[]
                {
                    new Vector2(tipX + 36f, 0f),
                    new Vector2(tipX - 3f, -18f),
                    new Vector2(tipX + 5f, 0f),
                    new Vector2(tipX - 3f, 18f),
                },
                steel);
            DrawLine(new Vector2(tipX - 1f, -19f), new Vector2(tipX + 17f, -31f), ochre, 5f, true);
            DrawLine(new Vector2(tipX - 1f, 19f), new Vector2(tipX + 17f, 31f), wind, 4f, true);
        }

        private void DrawGuardianShieldPoke(float t, float alpha)
        {
            float brace = EaseOutCubic(Math.Min(t * 2.05f, 1f));
            float thrust = EaseOutCubic(Math.Clamp((t - 0.12f) * 1.75f, 0f, 1f));
            float tipX = Mathf.Lerp(-38f, 105f, thrust);
            Color shield = WithAlpha(GuardianShieldBlue, alpha);
            Color steel = WithAlpha(GuardianSteel, alpha);
            Color wind = WithAlpha(GuardianWind, alpha * 0.8f);

            Vector2 shieldOffset = new(Mathf.Lerp(-95f, -58f, brace), 0f);
            Vector2[] shieldShape =
            {
                shieldOffset + new Vector2(-25f, -55f),
                shieldOffset + new Vector2(24f, -45f),
                shieldOffset + new Vector2(30f, 8f),
                shieldOffset + new Vector2(0f, 61f),
                shieldOffset + new Vector2(-30f, 8f),
            };
            DrawPolyline(
                new[]
                {
                    shieldShape[0],
                    shieldShape[1],
                    shieldShape[2],
                    shieldShape[3],
                    shieldShape[4],
                    shieldShape[0],
                },
                WithAlpha(GuardianShieldBlue, alpha * 0.3f),
                17f,
                true);
            DrawPolyline(
                new[]
                {
                    shieldShape[0],
                    shieldShape[1],
                    shieldShape[2],
                    shieldShape[3],
                    shieldShape[4],
                    shieldShape[0],
                },
                shield,
                6f,
                true);
            DrawLine(shieldOffset + new Vector2(0f, -38f), shieldOffset + new Vector2(0f, 38f), steel, 4f, true);

            DrawLine(new Vector2(-62f, 0f), new Vector2(tipX, 0f), WithAlpha(GuardianWind, alpha * 0.25f), 15f, true);
            DrawLine(new Vector2(-62f, 0f), new Vector2(tipX, 0f), steel, 5f, true);
            DrawColoredPolygon(
                new[]
                {
                    new Vector2(tipX + 31f, 0f),
                    new Vector2(tipX - 2f, -14f),
                    new Vector2(tipX + 5f, 0f),
                    new Vector2(tipX - 2f, 14f),
                },
                wind);
        }

        private static float SmoothStep(float from, float to, float value)
        {
            if (Math.Abs(to - from) < 0.0001f)
                return value >= to ? 1f : 0f;

            float t = Math.Clamp((value - from) / (to - from), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Math.Clamp(value, 0f, 1f);
            return 1f - inverse * inverse * inverse;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new(color.R, color.G, color.B, Math.Clamp(alpha, 0f, 1f));
    }
}
