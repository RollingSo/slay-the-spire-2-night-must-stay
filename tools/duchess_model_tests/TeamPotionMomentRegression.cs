using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Potions;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Patches;

internal static class TeamPotionMomentRegression
{
    internal static void Run()
    {
        PotionModel[] potions = [
            ModelDb.Potion<StalwartShieldGrease>(), ModelDb.Potion<KnightlySpirit>(),
            ModelDb.Potion<FearlessLiquor>(), ModelDb.Potion<PickledTurtleNeckMeat>(),
            ModelDb.Potion<StarlightShard>(), ModelDb.Potion<DuchessSmokeBottle>(),
            ModelDb.Potion<DuchessRadiantBladeCrystal>(), ModelDb.Potion<DuchessRegretPotion>(),
            ModelDb.Potion<WylderFireGrease>(), ModelDb.Potion<WylderBraveMeat>(),
            ModelDb.Potion<WylderStoneSwordKey>(),
        ];
        if (potions.Any(potion => potion.TargetType != TargetType.AnyPlayer))
            throw new Exception("Each listed potion must use the native self-or-teammate target.");

        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.team-potion-moment");
        harmony.Patch(typeof(PowerCmd).GetMethods().Single(method => method.Name == "Apply" && !method.IsGenericMethod),
            prefix: new HarmonyMethod(typeof(WylderBattleArtRegression), nameof(WylderBattleArtRegression.Apply)));
        try
        {
            var combat = new CombatState();
            var guardian = MakePlayer(ModelDb.Character<Guardian>(), combat);
            var wylder = MakePlayer(ModelDb.Character<Wylder>(), combat);
            Task start = Task.CompletedTask;
            DuchessMomentPatch.AttachAtCombatStart(combat, ref start);
            start.GetAwaiter().GetResult();
            var moment = guardian.Creature.GetPower<DuchessMomentPower>()
                ?? throw new Exception("Guardian did not receive a Moment controller.");
            if (!wylder.Creature.HasPower<DuchessMomentPower>())
                throw new Exception("Wylder did not receive a Moment controller.");

            var borrowed = ModelDb.Card<DuchessWaitAMoment>().ToMutable();
            borrowed.Owner = guardian;
            var defend = ModelDb.Card<WylderDefend>().ToMutable();
            defend.Owner = guardian;
            moment.AfterCardPlayedLate(null!, TrackingArrowRegression.MakePlay(defend, guardian)).GetAwaiter().GetResult();
            if (DuchessMomentPower.Current(guardian) != 1 || DuchessMomentPower.Current(wylder) != 0
                || !moment.TryModifyEnergyCostInCombat(borrowed, 1m, out decimal cost) || cost != 0m)
                throw new Exception("A borrowed Duchess card must use its non-Duchess owner's Moment.");
            moment.AfterPlayerTurnStartLate(null!, wylder).GetAwaiter().GetResult();
            if (DuchessMomentPower.Current(guardian) != 1)
                throw new Exception("A teammate's turn reset the wrong player's Moment.");
            moment.AfterPlayerTurnStartLate(null!, guardian).GetAwaiter().GetResult();
            if (DuchessMomentPower.Current(guardian) != 0)
                throw new Exception("Moment did not reset on the owner's turn.");

            var smoke = ModelDb.Potion<DuchessSmokeBottle>().ToMutable();
            AccessTools.PropertySetter(typeof(PotionModel), nameof(PotionModel.Owner))
                .Invoke(smoke, new object[] { guardian });
            ((Task)AccessTools.Method(typeof(DuchessSmokeBottle), "OnUse")
                .Invoke(smoke, new object?[] { null, wylder.Creature })!).GetAwaiter().GetResult();
            if (wylder.Creature.GetPowerAmount<DuchessConcealmentPower>() != 3
                || guardian.Creature.HasPower<DuchessConcealmentPower>())
                throw new Exception("Smoke Bottle must apply its effect to the targeted teammate.");
            Console.WriteLine("PASS: 11 team-potion target declarations, targeted Smoke Bottle, and independent Moment for non-Duchess players.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static Player MakePlayer(CharacterModel character, CombatState combat)
    {
        var player = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(player, character);
        var creature = new Creature(player, 70, 70) { CombatState = combat };
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        ((List<Creature>)AccessTools.Field(typeof(CombatState), "_allies").GetValue(combat)!).Add(creature);
        return player;
    }
}
