using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

// Use the native gold outline; querying a glow must never change the card's
// displayed target preview or consume any of the first-attack effects.
[HarmonyPatch(typeof(CardModel), "get_ShouldGlowGold")]
internal static class IroneyeCardGlowPatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        CardModel card = __instance;
        if (__result || card.Owner?.Character is not Ironeye
            || card.Pile?.Type != PileType.Hand || card.CombatState == null)
            return;

        var dealer = card.Owner.Creature;
        decimal distance = dealer.GetPower<DistancePower>()?.Amount ?? 0m;
        if (card is ReturnToZero || card is FrightenedBird)
        {
            decimal step = card.DynamicVars["Distance"].BaseValue;
            decimal next = decimal.Clamp(distance + (card is ReturnToZero ? -step : step), -5m, 5m);
            __result = distance != next && next == 0m
                && (card is ReturnToZero || card.CombatState.HittableEnemies.Any(e => e.IsAlive));
            return;
        }
        if (card.Type != CardType.Attack)
            return;

        foreach (var enemy in card.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive || !(enemy.GetPower<NightMustStayMarkPower>()?.Amount > 0m))
                continue;
            var vars = card.DynamicVars.Clone(card);
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal, enemy, vars);
            if (vars.Values.Any(v => v.Name == "CalculatedHits" && v.PreviewValue <= 0m))
                continue;
            var calculated = vars.Values.OfType<CalculatedDamageVar>().FirstOrDefault();
            var damage = vars.Values.OfType<DamageVar>().FirstOrDefault();
            decimal amount = calculated?.PreviewValue ?? damage?.PreviewValue ?? 0m;
            ValueProp props = calculated?.Props ?? damage?.Props ?? ValueProp.Unpowered;
            if (props.HasFlag(ValueProp.Move)
                && NightMustStayMarkPower.CanTriggerFromDamage(dealer, card, amount, true))
            {
                __result = true;
                return;
            }
        }
    }
}
