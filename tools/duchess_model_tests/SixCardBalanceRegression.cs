using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;

internal static class SixCardBalanceRegression
{
    public static void Run()
    {
        Check<GracefulBladeDance>(0, 0, 4, 6);
        Check<HeavenlyEyeForm>(2, 1, null, null);
        Check<AntiAirShot>(1, 1, 7, 8, 1, 2);
        Check<SpiritShot>(1, 1, 8, 11);
        Check<Scatter>(1, 1, 5, 5, 1, 2);
        Check<UnderworldRising>(2, 1, null, null);
        var scatter = ModelDb.Card<Scatter>();
        Assert(scatter.TargetType == TargetType.AllEnemies && !scatter.DynamicVars.ContainsKey("Distance"),
            "Scatter must target all enemies and no longer move Distance.");

        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.graceful-blade-dance");
        harmony.Patch(typeof(CardPileCmd).GetMethods().Single(method => method.Name == "Add"
            && method.GetParameters()[0].ParameterType == typeof(CardModel)
            && method.GetParameters()[1].ParameterType == typeof(PileType)),
            prefix: new HarmonyMethod(typeof(TrackingArrowRegression), nameof(TrackingArrowRegression.CaptureReturn)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var player = TrackingArrowRegression.MakePlayer();
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player,
                    new Creature(player, 70, 70) { CombatState = new CombatState() });
                var card = (GracefulBladeDance)ModelDb.Card<GracefulBladeDance>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                if (upgraded) card.UpgradeInternal();
                ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards")
                    .GetValue(player.PlayerCombatState!.DiscardPile)!).Add(card);
                var play = new CardPlay { Card = card, Player = player, Target = null,
                    ResultPile = PileType.Discard, Resources = default!, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
                card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
                Assert(card.Pile!.Type == PileType.Discard, "No Mark: no return.");
                card.OnMarkTriggered(4);
                card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
                Assert(card.Pile!.Type == PileType.Discard, "Mark outside own damage: no return.");
                AccessTools.Field(typeof(GracefulBladeDance), "_resolvingDamage").SetValue(card, true);
                card.OnMarkTriggered(4);
                AccessTools.Field(typeof(GracefulBladeDance), "_resolvingDamage").SetValue(card, false);
                card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
                Assert(card.Pile!.Type == PileType.Hand && card.EnergyCost.GetResolved() == 0,
                    "Own damage Mark must return without increasing cost.");
                card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
                Assert(card.Pile!.Cards.Count(c => c == card) == 1, "Return must not duplicate card.");
            }
            Console.WriteLine("PASS: six-card base/upgraded costs, damage, Mark stacks, Scatter AOE target, and Graceful Blade Dance own-damage-only return with no cost growth (movement visuals stubbed).");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Check<T>(int cost, int upgradedCost, decimal? damage, decimal? upgradedDamage,
        decimal? mark = null, decimal? upgradedMark = null) where T : CardModel
    {
        CardModel card = ModelDb.Card<T>().ToMutable();
        Assert(card.EnergyCost.GetResolved() == cost, typeof(T).Name + " cost.");
        if (damage != null) Assert(card.DynamicVars.Damage.BaseValue == damage, typeof(T).Name + " damage.");
        if (mark != null) Assert(card.DynamicVars["Mark"].BaseValue == mark, typeof(T).Name + " Mark.");
        if (card is GracefulBladeDance) Assert(card.Keywords.Contains(CardKeyword.Retain), "Blade Dance must retain.");
        card.UpgradeInternal();
        Assert(card.EnergyCost.GetResolved() == upgradedCost, typeof(T).Name + " upgraded cost.");
        if (upgradedDamage != null) Assert(card.DynamicVars.Damage.BaseValue == upgradedDamage, typeof(T).Name + " upgraded damage.");
        if (upgradedMark != null) Assert(card.DynamicVars["Mark"].BaseValue == upgradedMark, typeof(T).Name + " upgraded Mark.");
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
}
