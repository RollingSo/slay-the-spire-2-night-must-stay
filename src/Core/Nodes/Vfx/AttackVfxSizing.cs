#nullable enable
using System;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Presentation units at the combat canvas scale; never changes combat damage.</summary>
public static class AttackVfxSizing
{
    // Native vfx_poison_impact: about 400–450 x 340–520 visible pixels during
    // its main pulse (alpha > .10). Individual attacks retain their own aspect ratio.
    public static float Strength(decimal damage) =>
        1f - MathF.Exp(-(float)(Math.Clamp(damage, 8m, 10008m) - 8m) / 55f);

    public static float CounterScale(decimal damage) => 1.65f * (1f + .60f * Strength(damage));
    public static float HaloScale(decimal damage) => 1.6f * (1f + .95f * Strength(damage));
}
