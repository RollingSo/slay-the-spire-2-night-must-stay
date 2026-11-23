using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class RetreatingDefenseRegression
{
    private static int _removals;

    internal static void Run()
    {
        var card = ModelDb.Card<RetreatingDefense>().ToMutable();
        Check(card.DynamicVars.Block.BaseValue == 4 && card.DynamicVars["Discount"].BaseValue == 1
            && !card.Keywords.Contains(CardKeyword.Exhaust), "Retreating Defense base rules.");
        card.UpgradeInternal();
        Check(card.DynamicVars.Block.BaseValue == 6 && card.DynamicVars.Block.WasJustUpgraded
            && !card.DynamicVars["Discount"].WasJustUpgraded && !card.Keywords.Contains(CardKeyword.Exhaust),
            "Retreating Defense upgrades only Block.");

        var harmony = new HarmonyLib.Harmony("NightMustStay.RetreatingDefense.Regression");
        harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Remove), new[] { typeof(PowerModel) }),
            prefix: new HarmonyMethod(typeof(RetreatingDefenseRegression), nameof(Remove)));
        harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Decrement)),
            prefix: new HarmonyMethod(typeof(RetreatingDefenseRegression), nameof(Decrement)));
        harmony.Patch(AccessTools.Method(typeof(PowerModel), "Flash"),
            prefix: new HarmonyMethod(typeof(RetreatingDefenseRegression), nameof(SkipFlash)));
        _removals = 0;
        try
        {
            var owner = TrackingArrowRegression.MakePlayer();
            var ally = TrackingArrowRegression.MakePlayer();
            var creature = new Creature(owner, 70, 70);
            var allyCreature = new Creature(ally, 70, 70);
            AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(owner, creature);
            AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(ally, allyCreature);
            var power = (RetreatingDefensePower)ModelDb.Power<RetreatingDefensePower>().ToMutable();
            AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, creature);
            power.SetAmount(1, true);

            CardModel Make<T>(Player player) where T : CardModel
            {
                var model = ModelDb.Card<T>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(model, player);
                return model;
            }

            var attack = Make<StrikeGuardian>(owner);
            var skill = Make<DefendGuardian>(owner);
            var allyAttack = Make<StrikeGuardian>(ally);
            var xAttack = Make<ThousandWeightHalberd>(owner);
            Check(power.TryModifyEnergyCostInCombatLate(attack, 2, out decimal cost) && cost == 1
                && !power.TryModifyEnergyCostInCombatLate(skill, 2, out _)
                && !power.TryModifyEnergyCostInCombatLate(allyAttack, 2, out _)
                && !power.TryModifyEnergyCostInCombatLate(xAttack, 0, out _),
                "Only the owner's fixed-cost Attack is discounted.");
            power.SetAmount(2, true);
            Check(power.TryModifyEnergyCostInCombatLate(attack, 2, out cost) && cost == 1,
                "Two stacks discount the first Attack by exactly one energy.");
            power.BeforeCardPlayed(TrackingArrowRegression.MakePlay(skill, owner)).GetAwaiter().GetResult();
            power.BeforeCardPlayed(TrackingArrowRegression.MakePlay(allyAttack, ally)).GetAwaiter().GetResult();
            power.AfterSideTurnEnd(null!, creature.Side, new[] { allyCreature }).GetAwaiter().GetResult();
            Check(power.Amount == 2 && _removals == 0, "Teammate events and Skills leave both discounts available.");
            power.BeforeCardPlayed(TrackingArrowRegression.MakePlay(attack, owner)).GetAwaiter().GetResult();
            Check(power.Amount == 1, "The first Attack consumes one stack.");
            var replay = TrackingArrowRegression.MakePlay(attack, owner);
            AccessTools.Property(typeof(CardPlay), nameof(CardPlay.PlayIndex)).SetValue(replay, 1);
            power.BeforeCardPlayed(replay).GetAwaiter().GetResult();
            Check(power.Amount == 1, "A replay does not spend a second discount.");
            var secondAttack = Make<StrikeGuardian>(owner);
            Check(power.TryModifyEnergyCostInCombatLate(secondAttack, 1, out cost) && cost == 0,
                "The second Attack still gets its own discount.");
            power.BeforeCardPlayed(TrackingArrowRegression.MakePlay(secondAttack, owner)).GetAwaiter().GetResult();
            Check(power.Amount == 0 && !power.TryModifyEnergyCostInCombatLate(attack, 2, out _),
                "A third Attack receives no discount.");
            power.SetAmount(1, true);
            power.AfterSideTurnEnd(null!, creature.Side, new[] { creature }).GetAwaiter().GetResult();
            Check(_removals == 1, "The owner's turn end clears an unused discount.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: Retreating Defense 4/6 Block, owner-only Attack cost, stacking and turn expiration.");
    }

    private static bool Remove(ref Task __result)
    {
        _removals++;
        __result = Task.CompletedTask;
        return false;
    }

    private static bool Decrement(PowerModel __0, ref Task __result)
    {
        __0.SetAmount(__0.Amount - 1, true);
        __result = Task.CompletedTask;
        return false;
    }

    private static bool SkipFlash() => false;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
