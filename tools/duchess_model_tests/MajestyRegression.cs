using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

internal static class MajestyRegression
{
    public static void Run()
    {
        if (typeof(GuardianMajesty).Assembly.GetType("NightMustStay.Core.Models.Cards.StormAvatar") != null
            || typeof(GuardianMajesty).Assembly.GetType("NightMustStay.Core.Models.Power.StormAvatarPower") != null)
            throw new Exception("Storm Avatar was not completely removed.");
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.majesty");
        // Standalone fixtures have no localization singleton; filtering mod cards
        // uses canonical IDs, but still evaluates the Title getter.
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Title)),
            prefix: new HarmonyMethod(typeof(WhirlingTitleFixture), nameof(WhirlingTitleFixture.Prefix)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(CombatStateFixture)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var player = TrackingArrowRegression.MakePlayer();
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player,
                    new Creature(player, 70, 70) { CombatState = new CombatState() });
                var card = (GuardianMajesty)ModelDb.Card<GuardianMajesty>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                if (upgraded) card.UpgradeInternal();
                if (card.EnergyCost.Canonical != 1 || card.Type != CardType.Skill
                    || card.Rarity != CardRarity.Uncommon || card.TargetType != TargetType.Self)
                    throw new Exception("Majesty cost/type/rarity/target mismatch.");
                var hand = (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(player.PlayerCombatState!.Hand)!;
                var calculated = (CalculatedVar)card.DynamicVars["CalculatedStrengthLoss"];
                if (!ReferenceEquals(AccessTools.Field(typeof(CalculatedVar), "_owner").GetValue(calculated), card))
                    throw new Exception("Calculated variable is not bound to the mutable card.");
                for (int count = 0; count <= 3; count++)
                {
                    hand.Clear();
                    hand.Add(ModelDb.Card<ShieldPoke>().ToMutable());
                    for (int i = 0; i < count; i++) hand.Add(ModelDb.Card<DefendGuardian>().ToMutable());
                    calculated.UpdateCardPreview(card, CardPreviewMode.Normal, null!, false);
                    var multiplier = (Func<CardModel, Creature, decimal>)AccessTools.Field(typeof(CalculatedVar), "_multiplierCalc").GetValue(calculated)!;
                    if (multiplier(card, null!) != count)
                        throw new Exception($"Majesty multiplier mismatch: expected={count}, actual={multiplier(card, null!)}.");
                    if (calculated.PreviewValue != count * (upgraded ? 3 : 2)
                        || calculated.Calculate(null) != calculated.PreviewValue)
                        throw new Exception($"Majesty preview and resolution disagree: count={count}, upgraded={upgraded}, preview={calculated.PreviewValue}, resolved={calculated.Calculate(null)}, hand={PileType.Hand.GetPile(card.Owner).Cards.Count}, defendId={hand.Last().Id.Entry}, extra={card.DynamicVars["CalculationExtra"].BaseValue}.");
                }
            }
            var library = ModelDb.Card<GuardianMajesty>().ToMutable();
            library.DynamicVars["CalculatedStrengthLoss"].UpdateCardPreview(library, CardPreviewMode.Normal, null!, false);
            Console.WriteLine("PASS: Majesty base/upgraded targeting, 0/1/2/3 defensive cards, preview and ownerless compendium; Storm Avatar removed.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    public static bool CombatStateFixture(CardModel __instance, ref ICombatState __result)
    {
        __result = __instance.Owner?.Creature?.CombatState!;
        return false;
    }
}
