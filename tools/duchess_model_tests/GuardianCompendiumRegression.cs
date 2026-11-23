using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Relics;

internal static class GuardianCompendiumRegression
{
    internal static void Run()
    {
        var player = TrackingArrowRegression.MakePlayer();
        var creature = new Creature(player, 70, 70);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
        var deck = (List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(player.Deck)!;

        CardModel Add<T>() where T : CardModel
        {
            var card = ModelDb.Card<T>().ToMutable();
            AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
            deck.Add(card);
            return card;
        }

        var whirlingStrike = Add<WhirlingStrike>();
        var strike = Add<StrikeGuardian>();
        var defend = Add<DefendGuardian>();
        var relic = (TacticalCompendium)ModelDb.Relic<TacticalCompendium>().ToMutable();
        AccessTools.Property(typeof(RelicModel), nameof(RelicModel.Owner)).SetValue(relic, player);
        if (!relic.HasUponPickupEffect) throw new Exception("Compendium is a pickup effect.");

        var harmony = new HarmonyLib.Harmony("NightMustStay.GuardianCompendium.Regression");
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromDeckForEnchantment),
            new[] { typeof(IReadOnlyList<CardModel>), typeof(EnchantmentModel), typeof(int), typeof(CardSelectorPrefs) }),
            prefix: new HarmonyMethod(typeof(GuardianCompendiumRegression), nameof(Select)));
        try
        {
            relic.AfterObtained().GetAwaiter().GetResult();
            if (whirlingStrike.Enchantment is not Inky || strike.Enchantment is not Inky || defend.Enchantment != null)
                throw new Exception("Compendium must enchant only the selected attacks with Inky.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }

        if (!ModelDb.Contains(typeof(StrikeDummy)))
            AccessTools.Method(typeof(ModelDb), "Inject").Invoke(null, new object[] { typeof(StrikeDummy) });
        var dummy = ModelDb.Relic<StrikeDummy>().ToMutable();
        AccessTools.Property(typeof(RelicModel), nameof(RelicModel.Owner)).SetValue(dummy, player);
        var damageHook = AccessTools.DeclaredMethod(typeof(StrikeDummy), nameof(AbstractModel.ModifyDamageAdditive));
        object?[] parameters = { null, 5m, ValueProp.Move, creature, whirlingStrike, TrackingArrowRegression.MakePlay(whirlingStrike, player) };
        if ((decimal)damageHook.Invoke(dummy, parameters.Take(damageHook.GetParameters().Length).ToArray())! != 3m)
            throw new Exception("Whirling Strike does not receive the native Strike Dummy bonus.");

        foreach (var (locale, keyword) in new[] { ("zhs", "打击"), ("eng", "Strike"), ("jpn", "ストライク"), ("kor", "타격") })
        {
            using var data = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
            if (!data.RootElement.GetProperty("WHIRLING_STRIKE.title").GetString()!.Contains(keyword, StringComparison.Ordinal))
                throw new Exception($"{locale}: Whirling Strike title is missing {keyword}.");
        }
        Console.WriteLine("PASS: Compendium selects two attacks and applies Inky; Whirling Strike receives Strike Dummy damage and matches four localized Strike keywords.");
    }

    private static bool Select(IReadOnlyList<CardModel> cards, EnchantmentModel enchantment, int amount,
        CardSelectorPrefs prefs, ref Task<IEnumerable<CardModel>> __result)
    {
        if (cards.Count != 2 || cards.Any(card => card.Type != CardType.Attack)
            || enchantment is not Inky || amount != 1 || prefs.MinSelect != 2 || prefs.MaxSelect != 2)
            throw new Exception("Invalid Tactical Compendium selector.");
        __result = Task.FromResult<IEnumerable<CardModel>>(cards);
        return false;
    }
}
