using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class ReversalStepRegression
{
    private static decimal _distance;
    private static decimal _extraDexterity;
    private static decimal _multiplier = 1m;

    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.reversal-step");
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(InCombat)));
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifyBlock)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(BlockHooks)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var player = TrackingArrowRegression.MakePlayer();
                var creature = new Creature(player, 70, 70) { CombatState = new CombatState() };
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
                var distance = (DistancePower)ModelDb.Power<DistancePower>().ToMutable();
                typeof(PowerModel).GetProperty(nameof(PowerModel.Owner))!.SetValue(distance, creature);
                ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!).Add(distance);
                var card = (ReversalStep)ModelDb.Card<ReversalStep>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards")
                    .GetValue(player.PlayerCombatState!.Hand)!).Add(card);
                if (upgraded) card.UpgradeInternal();
                foreach (var (current, extraDexterity, multiplier, expected) in new (int, decimal, decimal, decimal)[]
                    { (0, 3, 1, 0), (1, 0, 1, 3), (-1, 0, 1, 5), (3, 0, 1, 9),
                        (-3, 0, 1, 15), (5, 2, 1, 17), (-5, 2, 1, 27), (3, 2, 0.75m, 8) })
                {
                    _distance = current; _extraDexterity = extraDexterity; _multiplier = multiplier;
                    distance.SetAmount(current, false);
                    card.DynamicVars.CalculatedBlock.UpdateCardPreview(card, CardPreviewMode.Normal, null!, true);
                    if (card.DynamicVars.CalculatedBlock.PreviewValue != expected)
                        throw new Exception($"Reversal Step preview: Distance {current}, expected {expected}, got {card.DynamicVars.CalculatedBlock.PreviewValue}.");
                    if (distance.Amount != current) throw new Exception("Preview changed actual Distance.");
                    card.DynamicVars.CalculatedBlock.UpdateCardPreview(card, CardPreviewMode.Normal, null!, false);
                    if (card.DynamicVars.CalculatedBlock.PreviewValue != 4m * Math.Abs(current))
                        throw new Exception("Raw block preview must match two points of movement per Distance at two Block each.");
                }
            }
            var libraryCard = ModelDb.Card<ReversalStep>().ToMutable();
            libraryCard.DynamicVars.CalculatedBlock.UpdateCardPreview(libraryCard, CardPreviewMode.Normal, null!, false);
            if (libraryCard.DynamicVars.CalculatedBlock.PreviewValue != 0) throw new Exception("Compendium preview must be safe without an owner.");
            Console.WriteLine("PASS: normal/upgraded Reversal Step previews at zero, positive/negative and maximum Distance; post-move Dexterity, Frail, unchanged combat state and ownerless compendium (block hooks simulated).");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    public static bool InCombat(ref bool __result) { __result = true; return false; }
    public static bool BlockHooks(object[] __args, ref decimal __result)
    {
        decimal raw = __args.OfType<decimal>().First();
        __result = Math.Max(0, Math.Floor((raw + _distance + _extraDexterity) * _multiplier));
        return false;
    }
}
