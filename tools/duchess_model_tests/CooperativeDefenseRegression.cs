using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

internal static class CooperativeDefenseRegression
{
    public static void Run()
    {
        if (typeof(CooperativeDefense).Assembly.GetType("NightMustStay.Core.Models.Cards.SharedGreatShield") != null)
            throw new Exception("Shared Great Shield must be completely removed.");
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.cooperative-defense");
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Title)),
            prefix: new HarmonyMethod(typeof(WhirlingTitleFixture), nameof(WhirlingTitleFixture.Prefix)));
        // Calculated previews require an active combat; this standalone fixture
        // has no global combat room. Keep the native calculation itself intact.
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatStateFixture)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatEndingFixture)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var player = TrackingArrowRegression.MakePlayer();
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player,
                    new Creature(player, 70, 70) { CombatState = new CombatState() });
                var card = (CooperativeDefense)ModelDb.Card<CooperativeDefense>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                if (upgraded) card.UpgradeInternal();
                if (card.EnergyCost.GetResolved() != 1
                    || card.Type != CardType.Skill || card.TargetType != TargetType.Self
                    || card.DynamicVars.Cards.BaseValue != (upgraded ? 2 : 1)
                    || card.DynamicVars["CalculationExtra"].BaseValue != 3
                    || card.DynamicVars["CalculationExtra"].WasJustUpgraded)
                    throw new Exception("Cooperative Defense stats/upgrade mismatch.");
                var hand = (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(player.PlayerCombatState!.Hand)!;
                foreach (int count in new[] { 0, 1, 3 })
                {
                    hand.Clear();
                    hand.Add(card); // The played card itself must not inflate its preview.
                    for (int i = 0; i < count; i++) hand.Add(ModelDb.Card<DefendGuardian>().ToMutable());
                    card.DynamicVars.CalculatedBlock.UpdateCardPreview(card, CardPreviewMode.Normal, null!, false);
                    if (card.DynamicVars.CalculatedBlock.PreviewValue != 3 * count
                        || card.DynamicVars.CalculatedBlock.Calculate(null) != 3 * count)
                        throw new Exception($"Cooperative Defense preview/resolution mismatch: {count} defenses, preview={card.DynamicVars.CalculatedBlock.PreviewValue}, resolution={card.DynamicVars.CalculatedBlock.Calculate(null)}.");
                }
                var wing = (WingStrike)ModelDb.Card<WingStrike>().ToMutable();
                if (upgraded) wing.UpgradeInternal();
                if (wing.DynamicVars.Damage.BaseValue != (upgraded ? 7 : 6)
                    || wing.DynamicVars.Cards.BaseValue != (upgraded ? 3 : 2))
                    throw new Exception("Wing Strike upgrade must change damage and draw count.");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
