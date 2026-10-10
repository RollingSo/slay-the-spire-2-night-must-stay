using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

internal static class GuardianTeamSkillsRegression
{
    public static void Run()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var offensive = ModelDb.Card<TurnTheOffensive>().ToMutable();
            var formation = ModelDb.Card<SacredFeatherFormation>().ToMutable();
            if (upgraded) { offensive.UpgradeInternal(); formation.UpgradeInternal(); }
            foreach (var card in new[] { offensive, formation })
                if (card.Type != CardType.Skill || card.TargetType != TargetType.Self
                    || card.EnergyCost.Canonical != 1
                    || card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
                    throw new Exception("Guardian team skill metadata mismatch.");
            if (offensive.Rarity != CardRarity.Uncommon || formation.Rarity != CardRarity.Rare
                || offensive.DynamicVars["GuardCounter"].BaseValue != (upgraded ? 16 : 12)
                || formation.DynamicVars["Regen"].BaseValue != (upgraded ? 5 : 4)
                || formation.DynamicVars["DrawReduction"].BaseValue != 2
                || formation.DynamicVars["DrawReduction"].WasJustUpgraded
                || !formation.Keywords.Contains(CardKeyword.Exhaust)
                || offensive.Keywords.Contains(CardKeyword.Exhaust))
                throw new Exception("Guardian team skill base/upgrade values or keywords mismatch.");
        }
        var owner = TrackingArrowRegression.MakePlayer();
        var other = TrackingArrowRegression.MakePlayer();
        var creature = new Creature(owner, 70, 70);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(owner, creature);
        var penalty = (SacredFeatherDrawReductionPower)ModelDb.Power<SacredFeatherDrawReductionPower>().ToMutable();
        typeof(PowerModel).GetProperty(nameof(PowerModel.Owner))!.SetValue(penalty, creature);
        penalty.SetAmount(2, false);
        penalty.AmountOnTurnStart = 0;
        if (penalty.ModifyHandDraw(owner, 5) != 5) throw new Exception("Penalty activated in the same turn it was applied.");
        penalty.AmountOnTurnStart = 2;
        if (penalty.ModifyHandDraw(owner, 5) != 3 || penalty.ModifyHandDraw(owner, 1) != 0
            || penalty.ModifyHandDraw(other, 5) != 5)
            throw new Exception("Penalty must reduce only its owner's next initial draw, clamped to zero.");
        penalty.SetAmount(4, false);
        penalty.AmountOnTurnStart = 4;
        if (penalty.ModifyHandDraw(owner, 5) != 1) throw new Exception("Repeated formation penalties must stack.");
        Console.WriteLine("PASS: Guardian team skill values/keywords/multiplayer constraints; owner-only next-turn draw boundary, zero clamp and stacking.");
    }
}
