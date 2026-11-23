using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class HoldTheLineRegression
{
    private static PowerModel? _removed;

    internal static void Run()
    {
        var harmony = new HarmonyLib.Harmony("NightMustStay.HoldTheLine.Regression");
        harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Remove), new[] { typeof(PowerModel) }),
            prefix: new HarmonyMethod(typeof(HoldTheLineRegression), nameof(Remove)));
        harmony.Patch(AccessTools.Method(typeof(PowerModel), "Flash"),
            prefix: new HarmonyMethod(typeof(HoldTheLineRegression), nameof(SkipFlash)));
        try
        {
            Player owner = TrackingArrowRegression.MakePlayer();
            Player ally = TrackingArrowRegression.MakePlayer();
            var creature = new Creature(owner, 70, 70);
            var allyCreature = new Creature(ally, 70, 70);
            var power = (HoldTheLinePower)ModelDb.Power<HoldTheLinePower>().ToMutable();
            AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(power, creature);

            CardPlay Play<T>(Player player) where T : CardModel
            {
                var card = ModelDb.Card<T>().ToMutable();
                AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                return TrackingArrowRegression.MakePlay(card, player);
            }

            var grantingPlay = Play<HoldTheLine>(owner);
            power.GrantingPlay = grantingPlay;
            var attack = Play<StrikeGuardian>(owner);
            var defend = Play<DefendGuardian>(owner);
            var allyDefend = Play<DefendGuardian>(ally);

            decimal Factor(Creature target, CardPlay play, ValueProp props = ValueProp.Move) =>
                power.ModifyBlockMultiplicative(target, 5m, props, play.Card, play);

            power.AfterCardPlayed(null!, grantingPlay).GetAwaiter().GetResult();
            power.AfterCardPlayed(null!, attack).GetAwaiter().GetResult();
            Check(_removed == null && Factor(creature, defend) == 2m, "Non-Block plays leave the effect ready.");
            Check(Factor(creature, grantingPlay) == 1m && Factor(allyCreature, defend) == 1m
                && Factor(creature, allyDefend) == 1m && Factor(creature, defend, ValueProp.Unpowered) == 1m
                && power.ModifyBlockMultiplicative(creature, 5m, ValueProp.Move, null!, null!) == 1m,
                "The granting play, teammates and indirect Block do not qualify.");
            Check(power.ModifyBlockMultiplicative(creature, 5m, ValueProp.Move, defend.Card, null!) == 2m,
                "The pending effect is visible in Block previews.");

            power.AfterModifyingBlockAmount(0m, defend.Card, defend).GetAwaiter().GetResult();
            power.AfterCardPlayed(null!, defend).GetAwaiter().GetResult();
            Check(_removed == null, "Zero Block does not consume the effect.");

            var nextPlay = TrackingArrowRegression.MakePlay(defend.Card, owner);
            power.AfterModifyingBlockAmount(10m, defend.Card, defend).GetAwaiter().GetResult();
            Check(Factor(creature, defend) == 2m && Factor(creature, nextPlay) == 1m,
                "The multiplier remains active for all Block from the triggering play.");
            power.AfterCardPlayed(null!, attack).GetAwaiter().GetResult();
            Check(_removed == null, "An unrelated play cannot consume the triggered effect.");
            power.AfterCardPlayed(null!, defend).GetAwaiter().GetResult();
            Check(ReferenceEquals(_removed, power), "The effect is removed after the triggering play.");
            Console.WriteLine("PASS: Hold the Line waits for card Block, supports previews, ignores zero and indirect Block, and expires after that play.");
        }
        finally
        {
            _removed = null;
            harmony.UnpatchAll(harmony.Id);
        }
    }

    private static bool Remove(PowerModel power, ref Task __result)
    {
        _removed = power;
        __result = Task.CompletedTask;
        return false;
    }

    private static bool SkipFlash() => false;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
