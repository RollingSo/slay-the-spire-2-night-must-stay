using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

internal static class WylderLatestBatchRegression
{
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }

    public static void Run()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var seamless = ModelDb.Card<WylderSeamless>().ToMutable();
            var moonlight = ModelDb.Card<WylderMoonlightSword>().ToMutable();
            var twin = ModelDb.Card<WylderTwinToken>().ToMutable();
            if (upgraded)
            {
                seamless.UpgradeInternal();
                moonlight.UpgradeInternal();
                twin.UpgradeInternal();
            }

            Check(seamless.Type == CardType.Power && seamless.Rarity == CardRarity.Rare
                && seamless.EnergyCost.GetResolved() == 1
                && seamless.DynamicVars.Block.BaseValue == (upgraded ? 14 : 10), "Seamless metadata and upgrade");
            Check(moonlight.Type == CardType.Power && moonlight.Rarity == CardRarity.Rare
                && moonlight.EnergyCost.GetResolved() == 1
                && moonlight.DynamicVars["Freeze"].BaseValue == (upgraded ? 3 : 2), "Moonlight Sword metadata and upgrade");
            Check(twin.Type == CardType.Skill && twin.Rarity == CardRarity.Uncommon
                && twin.TargetType == TargetType.AnyPlayer
                && twin.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly
                && twin.EnergyCost.GetResolved() == (upgraded ? 0 : 1), "Twin Token metadata and upgrade");
        }

        Check(ModelDb.Card<WylderHoarfrostStomp>() is WylderFollowThroughCard,
            "Hoarfrost Stomp must participate in Follow Through");

        var owner = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(owner, ModelDb.Character<Wylder>());
        var creature = new Creature(owner, 50, 50) { CombatState = new CombatState() };
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(owner, creature);
        var seamlessPower = (WylderSeamlessPower)ModelDb.Power<WylderSeamlessPower>().ToMutable();
        AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(seamlessPower, creature);
        seamlessPower.SetAmount(2, true);
        seamlessPower.UpgradedCopies = 1;
        Check(seamlessPower.Sum(10, 14) == 24, "Seamless mixed copies preserve upgraded contribution");
        var moonlightPower = (WylderMoonlightSwordPower)ModelDb.Power<WylderMoonlightSwordPower>().ToMutable();
        AccessTools.Property(typeof(PowerModel), nameof(PowerModel.Owner)).SetValue(moonlightPower, creature);
        moonlightPower.SetAmount(2, true);
        moonlightPower.UpgradedCopies = 1;
        Check(moonlightPower.BasicCopies == 1 && moonlightPower.Copies == 2,
            "Moonlight Sword mixed copies preserve individual triggers");

        ((List<PowerModel>)AccessTools.Field(typeof(Creature), "_powers").GetValue(creature)!).Add(seamlessPower);
        var hand = CardPile.Get(PileType.Hand, owner) ?? throw new Exception("Missing test hand pile");
        var lastCard = ModelDb.Card<WylderStrike>().ToMutable();
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(lastCard, owner);
        hand.AddInternal(lastCard, silent: true);
        var play = TrackingArrowRegression.MakePlay(lastCard, owner);
        var handContext = typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderHandPlayContext")!;
        AccessTools.Method(handContext, "CaptureWrapper").Invoke(null, new object[] { lastCard });
        AccessTools.Method(handContext, "Record").Invoke(null, new object[] { play });
        WylderCombatEvents.BeginCard(play);
        var ledger = AccessTools.Field(typeof(WylderCombatEvents), "Plays").GetValue(null)!;
        object?[] lookup = { play, null };
        ledger.GetType().GetMethod("TryGetValue")!.Invoke(ledger, lookup);
        Check((int)AccessTools.Field(lookup[1]!.GetType(), "SeamlessBlock").GetValue(lookup[1])! == 24,
            "Seamless snapshots the exact pre-play last-card state and mixed-copy Block");

        foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
        {
            using var cards = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
            using var powers = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/powers.json"));
            foreach (string id in new[] { "WYLDER_SEAMLESS", "WYLDER_MOONLIGHT_SWORD", "WYLDER_TWIN_TOKEN" })
            {
                Check(cards.RootElement.TryGetProperty(id + ".title", out _), $"{locale} {id} title");
                Check(cards.RootElement.TryGetProperty(id + ".description", out _), $"{locale} {id} description");
                Check(cards.RootElement.TryGetProperty(id + ".upgradeDescription", out _), $"{locale} {id} upgrade description");
            }
            foreach (string id in new[] { "WYLDER_SEAMLESS_POWER", "WYLDER_MOONLIGHT_SWORD_POWER" })
            {
                Check(powers.RootElement.TryGetProperty(id + ".title", out _), $"{locale} {id} title");
                Check(powers.RootElement.TryGetProperty(id + ".description", out _), $"{locale} {id} description");
            }
        }

        Console.WriteLine("PASS: latest Wylder cards, upgrades, Follow Through inheritance, mixed power copies and localization parity.");
    }
}
