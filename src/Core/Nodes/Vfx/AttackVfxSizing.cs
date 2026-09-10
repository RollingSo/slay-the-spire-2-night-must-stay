#nullable enable
using System;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Presentation units at the combat canvas scale; never changes combat damage.</summary>
public static class AttackVfxSizing
{
    // Native poison is an area-effect reference, not a minimum for every attack.
    // ParticleAttackVfx applies per-effect artwork scale; growth is deliberately bounded.
    public static float Strength(decimal damage) =>
        1f - MathF.Exp(-(float)(Math.Clamp(damage, 8m, 10008m) - 8m) / 55f);

    public static float CounterScale(decimal damage) => 1.65f * (1f + .35f * Strength(damage));
    public static float HaloScale(decimal damage) => 1.6f * (1f + .45f * Strength(damage));
}
