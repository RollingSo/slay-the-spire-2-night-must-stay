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
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatStateFixture)));
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifyBlock)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(BlockHooks)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                CardModel Mutable<T>() where T : CardModel
                {
                    var model = ModelDb.Card<T>().ToMutable();
                    if (upgraded) model.UpgradeInternal();
                    return model;
                }
                if (Mutable<BowLikeFullMoon>().EnergyCost.GetResolved() != (upgraded ? 0 : 1)
                    || Mutable<BladeShadowUnmatched>().EnergyCost.GetResolved() != (upgraded ? 0 : 1))
                    throw new Exception("Moon/Blade costs must be 1/0.");
                var poison = Mutable<PoisonScheme>();
                if (poison.DynamicVars["HiddenPoison"].BaseValue != (upgraded ? 4 : 3)
                    || poison.DynamicVars["RetainCards"].BaseValue != (upgraded ? 3 : 2))
                    throw new Exception("Poison Scheme upgrade must improve poison and retention.");
                var star = Mutable<StarPlucker>();
                if (star.DynamicVars["Cards"].BaseValue != (upgraded ? 3 : 2)
                    || !star.Keywords.Contains(CardKeyword.Exhaust))
                    throw new Exception("Star Plucker must draw 2/3 and Exhaust.");
                var shadow = Mutable<IroneyeShadowAssault>();
                if (shadow.DynamicVars["Distance"].BaseValue != -3
                    || shadow.DynamicVars.Damage.BaseValue != (upgraded ? 12 : 9)
                    || shadow.Rarity != CardRarity.Common || shadow.TargetType != TargetType.AnyEnemy
                    || !shadow.Keywords.Contains(CardKeyword.Retain))
                    throw new Exception("Shadow Assault stats/keywords mismatch.");
                if (Mutable<HundredSchemes>().DynamicVars.Cards.BaseValue != 2
                    || Mutable<RoadAlreadyTraveled>().Rarity != CardRarity.Uncommon)
                    throw new Exception("Hundred Schemes count/Road rarity mismatch.");
                if (Mutable<BowCombatArt>().DynamicVars.Damage.BaseValue != (upgraded ? 12 : 9)
                    || Mutable<Offensive>().DynamicVars.Damage.BaseValue != 6
                    || Mutable<GroundSkid>().DynamicVars.Block.BaseValue != 5
                    || Mutable<FrightenedBird>().EnergyCost.GetResolved() != 0)
                    throw new Exception("Ironeye damage/block/cost balance mismatch.");
            }
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
                var road = (RoadAlreadyTraveled)ModelDb.Card<RoadAlreadyTraveled>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(road, player);
                if (upgraded) road.UpgradeInternal();
                if (road.DynamicVars["CalculationBase"].BaseValue != 6
                    || road.DynamicVars["CalculationBase"].WasJustUpgraded
                    || road.DynamicVars["CalculationExtra"].BaseValue != (upgraded ? 3 : 2))
                    throw new Exception("Road upgrade must change only per-Distance block.");
                foreach (int moved in new[] { 0, 1, 5 })
                {
                    distance.SetAmount(0, false);
                    var data = AccessTools.Method(typeof(PowerModel), "GetInternalData")
                        .MakeGenericMethod(typeof(DistancePower).GetNestedType("Data", BindingFlags.NonPublic)!)
                        .Invoke(distance, null)!;
                    AccessTools.Field(data.GetType(), "DistanceMovedThisTurn").SetValue(data, (decimal)moved);
                    road.DynamicVars.CalculatedBlock.UpdateCardPreview(road, CardPreviewMode.Normal, null!, false);
                    decimal expectedRoad = 6 + moved * (upgraded ? 3 : 2);
                    if (road.DynamicVars.CalculatedBlock.PreviewValue != expectedRoad
                        || road.DynamicVars.CalculatedBlock.Calculate(null) != expectedRoad)
                        throw new Exception($"Road dynamic block preview/resolution mismatch: moved={moved}, preview={road.DynamicVars.CalculatedBlock.PreviewValue}, resolved={road.DynamicVars.CalculatedBlock.Calculate(null)}, expected={expectedRoad}.");
                }
                foreach (var (current, extraDexterity, multiplier, expected) in new (int, decimal, decimal, decimal)[]
                    { (0, 3, 1, 0), (1, 0, 1, 5), (-1, 0, 1, 7), (3, 0, 1, 15),
                        (-3, 0, 1, 21), (5, 2, 1, 27), (-5, 2, 1, 37), (3, 2, 0.75m, 12) })
                {
                    _distance = current; _extraDexterity = extraDexterity; _multiplier = multiplier;
                    distance.SetAmount(current, false);
                    card.DynamicVars.CalculatedBlock.UpdateCardPreview(card, CardPreviewMode.Normal, null!, true);
                    if (card.DynamicVars.CalculatedBlock.PreviewValue != expected)
                        throw new Exception($"Reversal Step preview: Distance {current}, expected {expected}, got {card.DynamicVars.CalculatedBlock.PreviewValue}.");
                    if (distance.Amount != current) throw new Exception("Preview changed actual Distance.");
                    card.DynamicVars.CalculatedBlock.UpdateCardPreview(card, CardPreviewMode.Normal, null!, false);
                    if (card.DynamicVars.CalculatedBlock.PreviewValue != 6m * Math.Abs(current))
                        throw new Exception("Raw block preview must match two points of movement per Distance at three Block each.");
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
