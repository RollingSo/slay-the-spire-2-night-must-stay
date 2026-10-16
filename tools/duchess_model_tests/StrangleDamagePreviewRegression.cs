using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Compatibility;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class StrangleDamagePreviewRegression
{
    private static decimal _strength;
    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("night-must-stay.tests.strangle-damage-preview");
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        harmony.Patch(AccessTools.Method(typeof(Sts2BranchCompat), nameof(Sts2BranchCompat.ModifyDamage)),
            prefix: new HarmonyMethod(typeof(StrangleDamagePreviewRegression), nameof(Damage)));
        try
        {
            foreach (bool upgraded in new[] { false, true })
            foreach (bool blade in new[] { false, true })
            foreach (bool strangle in new[] { false, true })
            foreach (int current in new[] { -5, -1, 0, 1, 5 })
            {
                var player = TrackingArrowRegression.MakePlayer();
                var creature = new Creature(player, 70, 70) { CombatState = new CombatState() };
                AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
                var powers = (List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!;
                void Add<T>() where T : PowerModel
                {
                    var power = ModelDb.Power<T>().ToMutable();
                    typeof(PowerModel).GetProperty(nameof(PowerModel.Owner))!.SetValue(power, creature);
                    powers.Add(power);
                }
                Add<DistancePower>();
                creature.GetPower<DistancePower>()!.SetAmount(current, false);
                if (blade) Add<BladeShadowUnmatchedPower>();
                if (strangle) Add<StrangleCommandPower>();
                decimal Contribution(int distance) => distance >= 0 ? -distance
                    : -distance * (blade ? 2 : 1) * (strangle ? 2 : 1);
                _strength = 2 + Contribution(current); // Preserve unrelated Strength.
                foreach (CardModel card in new CardModel[]
                    { ModelDb.Card<TurningArrow>().ToMutable(), ModelDb.Card<BladeGlide>().ToMutable() })
                {
                    AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
                    if (upgraded) card.UpgradeInternal();
                    decimal raw = card is TurningArrow
                        ? 6 + 2 * Math.Abs(current) * (upgraded ? 3 : 2)
                        : Math.Abs(current) * (upgraded ? 3 : 2);
                    int future = card is TurningArrow ? -current : 0;
                    decimal expected = Math.Max(0, raw + 2 + Contribution(future));
                    card.DynamicVars.CalculatedDamage.UpdateCardPreview(card, CardPreviewMode.Normal, null!, true);
                    if (card.DynamicVars.CalculatedDamage.PreviewValue != expected)
                        throw new Exception($"{card.GetType().Name}: Distance {current}, blade={blade}, strangle={strangle}, expected {expected}, got {card.DynamicVars.CalculatedDamage.PreviewValue}.");
                    if (creature.GetPower<DistancePower>()!.Amount != current)
                        throw new Exception("Damage preview mutated Distance.");
                }
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    public static bool Damage(object[] __args, ref decimal __result)
    {
        __result = (decimal)__args[4] + _strength;
        __args[9] = Array.Empty<AbstractModel>();
        return false;
    }
}
