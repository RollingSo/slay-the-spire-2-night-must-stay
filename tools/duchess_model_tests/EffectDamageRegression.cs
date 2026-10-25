using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

internal static class EffectDamageRegression
{
    public static void Run()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var lightning = ModelDb.Card<LightningStrike>().ToMutable();
            var restage = ModelDb.Card<DuchessReenactment>().ToMutable();
            var attack = ModelDb.Card<PreciseLightningStrike>().ToMutable();
            if (upgraded)
            {
                lightning.UpgradeInternal();
                restage.UpgradeInternal();
                attack.UpgradeInternal();
            }
            AssertEffect(lightning.DynamicVars.Damage.Props);
            AssertEffect(restage.DynamicVars.CalculatedDamage.Props);
            if (lightning.DynamicVars.Damage.BaseValue != (upgraded ? 9m : 6m))
                throw new Exception("Lightning Strike damage/upgrade changed.");
            if (!attack.DynamicVars.Damage.Props.IsPoweredAttack())
                throw new Exception("Normal Revenant attacks must retain attack modifiers.");
        }
    }

    private static void AssertEffect(ValueProp props)
    {
        if (props.IsPoweredAttack() || props.HasFlag(ValueProp.Move)
            || !props.HasFlag(ValueProp.Unpowered) || props.HasFlag(ValueProp.Unblockable))
            throw new Exception("Effect damage must ignore attack modifiers but remain blockable.");
    }
}
