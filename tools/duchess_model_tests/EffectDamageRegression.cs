using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using System.Reflection;

internal static class EffectDamageRegression
{
    public static void Run()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var lightning = ModelDb.Card<LightningStrike>().ToMutable();
            var restage = ModelDb.Card<DuchessReenactment>().ToMutable();
            var fallingMagic = ModelDb.Card<DuchessFallingMagic>().ToMutable();
            var attack = ModelDb.Card<PreciseLightningStrike>().ToMutable();
            if (upgraded)
            {
                lightning.UpgradeInternal();
                restage.UpgradeInternal();
                fallingMagic.UpgradeInternal();
                attack.UpgradeInternal();
            }
            AssertAttack(lightning.DynamicVars.Damage.Props);
            AssertEffect(restage.DynamicVars.CalculatedDamage.Props);
            AssertEffect((ValueProp)typeof(DuchessCard)
                .GetProperty("ShuffleAoeDamageProps", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!);
            if (lightning.DynamicVars.Damage.BaseValue != (upgraded ? 9m : 6m))
                throw new Exception("Lightning Strike damage/upgrade changed.");
            if (fallingMagic.DynamicVars["ShuffleAoeDamage"].BaseValue != (upgraded ? 16m : 12m))
                throw new Exception("Falling Magic damage/upgrade changed.");
            if (!attack.DynamicVars.Damage.Props.IsPoweredAttack())
                throw new Exception("Normal Revenant attacks must retain attack modifiers.");
        }
    }

    private static void AssertAttack(ValueProp props)
    {
        if (!props.IsPoweredAttack() || !props.HasFlag(ValueProp.Move)
            || props.HasFlag(ValueProp.Unpowered))
            throw new Exception("Lightning Strike must receive attack damage modifiers.");
    }

    private static void AssertEffect(ValueProp props)
    {
        if (props.IsPoweredAttack() || props.HasFlag(ValueProp.Move)
            || !props.HasFlag(ValueProp.Unpowered) || props.HasFlag(ValueProp.Unblockable))
            throw new Exception("Effect damage must ignore attack modifiers but remain blockable.");
    }
}
