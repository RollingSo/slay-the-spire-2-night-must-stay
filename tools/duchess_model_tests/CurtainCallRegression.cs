using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

internal static class CurtainCallRegression
{
    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.curtain-call");
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var player = TrackingArrowRegression.MakePlayer();
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player,
                    new Creature(player, 70, 70) { CombatState = new CombatState() });
                var card = ModelDb.Card<CurtainCall>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                if (upgraded) card.UpgradeInternal();
                card.AddKeyword(CardKeyword.Retain); // Must not count itself while hovered.
                var hand = (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards")
                    .GetValue(player.PlayerCombatState!.Hand)!;
                foreach (int count in new[] { 0, 1, 4 })
                {
                    hand.Clear(); hand.Add(card);
                    for (int i = 0; i < count; i++)
                    {
                        var retained = ModelDb.Card<Offensive>().ToMutable();
                        if (i % 2 == 0) retained.AddKeyword(CardKeyword.Retain);
                        else AccessTools.PropertySetter(typeof(CardModel), "HasSingleTurnRetain").Invoke(retained, new object[] { true });
                        hand.Add(retained);
                    }
                    hand.Add(ModelDb.Card<Offensive>().ToMutable());
                    var hits = (CalculatedVar)card.DynamicVars["CalculatedHits"];
                    hits.UpdateCardPreview(card, CardPreviewMode.Normal, null!, false);
                    if (hits.PreviewValue != count || hits.Calculate(null) != count)
                        throw new Exception("Curtain Call hit preview/calculation mismatch.");
                    if (card.DynamicVars.Damage.BaseValue != (upgraded ? 8 : 5))
                        throw new Exception("Curtain Call damage upgrade changed.");
                }
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
