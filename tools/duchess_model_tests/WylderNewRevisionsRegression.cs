using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
using SmartFormat;
using SmartFormat.Extensions;

internal static class WylderNewRevisionsRegression
{
    private static readonly Type Shared = typeof(WylderBattleArtRegression);
    private static Player Owner => (Player)AccessTools.Field(Shared, "_owner").GetValue(null)!;
    private static Creature[] Enemies => (Creature[])AccessTools.Field(Shared, "_enemies").GetValue(null)!;
    private static List<CardModel> Generated => (List<CardModel>)AccessTools.Field(Shared, "Generated").GetValue(null)!;
    private static List<decimal> Damage => (List<decimal>)AccessTools.Field(Shared, "ResolvedDamage").GetValue(null)!;
    private static List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)> Rewards =>
        (List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)>)AccessTools.Field(Shared, "Rewards").GetValue(null)!;
    private static int _checks, _auto;
    internal static void Run()
    {
        var h = new HarmonyLib.Harmony("NightMustStay.Wylder.NewRevisions.Regression");
        h.Patch(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.AutoPlay)), prefix: new HarmonyMethod(typeof(WylderNewRevisionsRegression), nameof(AutoPlay)));
        try
        {
            foreach (bool up in new[] { false, true })
            foreach (bool pursuit in new[] { false, true })
            {
                Fixture(pursuit);
                var flame = Card<WylderFlameChaseSlash>(up);
                Check(flame.EnergyCost.GetResolved() == 1 && flame.Rarity == CardRarity.Uncommon && flame.Type == CardType.Attack, "Flame Chase metadata");
                Play(flame);
                Check(Damage.Single() == Math.Floor((up ? 9m : 7m) * (pursuit ? 1.5m : 1m)), "Flame Chase damage and Pursuit bonus");
                var coating = Generated.Single();
                Check(coating is WylderFireCoating && coating.IsUpgraded == up && coating.DynamicVars["Vigor"].BaseValue == (up ? 6 : 4), "Generated correct Fire Coating version");
                Check(!Owner.Creature.HasPower<WylderPursuitPower>(), "Flame Chase consumes old Pursuit normally");
                var followup = Card<WylderStrike>(false); Play(followup);
                Check(Damage.Last() == 6 && !Owner.Creature.HasPower<WylderPursuitPower>(), "Next attack gets no Pursuit bonus");

                Fixture(pursuit); var stake = Card<WylderAssaultStake>(up);
                Check(stake.EnergyCost.GetResolved() == 3 && stake.Rarity == CardRarity.Rare && stake.TargetType == TargetType.AllEnemies, "Assault Stake 3-cost rare AOE");
                Check(stake.Keywords.Contains(CardKeyword.Exhaust) && stake.Keywords.Contains(CardKeyword.Retain) == up, "Exhaust and upgraded Retain");
                Play(stake);
                Check(Damage.SequenceEqual(new[] { pursuit ? 42m : 28m, pursuit ? 42m : 28m }), "AOE hits both enemies with fixed28 and Pursuit");
                Check(Rewards.Count(r => r.Kind == "Stun") == 2 && Rewards.All(r => r.Kind == "Stun"), "Stuns all living enemies after damage, no obsolete rewards");

                Fixture(pursuit); stake = Card<WylderAssaultStake>(up); Put(stake, PileType.Hand);
                Owner.PlayerCombatState!.Energy = 0;
                stake.BeforeSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
                Check(_auto == 1 && stake.Pile?.Type == PileType.Exhaust && Owner.PlayerCombatState.Energy == 0, "Sole card autoplays and exhausts without energy");
                Fixture(); stake = Card<WylderAssaultStake>(up); Put(stake, PileType.Hand); Put(Card<WylderStrike>(false), PileType.Hand);
                stake.BeforeSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
                Check(_auto == 0 && stake.Pile?.Type == PileType.Hand, "Two-card hand cannot trigger Desperation");
            }
            Fixture(); var lethal = Card<WylderAssaultStake>(false); Enemies[0].SetCurrentHpInternal(0); Play(lethal);
            Check(Rewards.Count(r => r.Kind == "Stun") == 1 && Rewards.Single().Target == Enemies[1], "Dead enemy excluded from stun");
            HoverPreviews(h);
            LocalizedText();
            Console.WriteLine($"PASS: {_checks} Flame Chase and Assault Stake checks: upgrades, generation, pursuit consumption, AOE, stun and sole-hand autoplay.");
        }
        finally { h.UnpatchAll(h.Id); }
    }
    private static void Fixture(bool pursuit = false)
    { AccessTools.Method(Shared, "Fixture").Invoke(null, new object[] { pursuit }); _auto = 0; }
    private static T Card<T>(bool up) where T : CardModel => (T)AccessTools.Method(Shared, "Card").Invoke(null, new object[] { ModelDb.Card<T>(), up })!;
    private static void Put(CardModel card, PileType pile) => AccessTools.Method(Shared, "Put").Invoke(null, new object[] { card, pile });
    private static void Play(CardModel card) => AccessTools.Method(Shared, "Play").Invoke(null, new object?[] { card, true, false, 0, null, null });
    public static bool AutoPlay(CardModel card, ref Task __result)
    {
        if (card is not WylderAssaultStake) return true;
        _auto++; Play(card); Put(card, PileType.Exhaust); __result = Task.CompletedTask; return false;
    }
    private static void HoverPreviews(HarmonyLib.Harmony h)
    {
        void Stub(MethodBase method, string name) => h.Patch(method, prefix: new HarmonyMethod(typeof(CardGlossaryRegression), name));
        Stub(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.FromPower), new[] { typeof(PowerModel), typeof(int?) }), nameof(CardGlossaryRegression.Power));
        Stub(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.ForEnergy), new[] { typeof(CardModel) }), nameof(CardGlossaryRegression.Energy));
        Stub(AccessTools.Method(typeof(LocString), nameof(LocString.GetRawText)), nameof(CardGlossaryRegression.Raw));
        Stub(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText)), nameof(CardGlossaryRegression.Formatted));
        foreach (bool up in new[] { false, true })
        {
            Fixture(); var card = Card<WylderFlameChaseSlash>(up);
            var tips = (IEnumerable<IHoverTip>)AccessTools.PropertyGetter(typeof(WylderFlameChaseSlash), "ExtraHoverTips").Invoke(card, null)!;
            var preview = tips.OfType<CardHoverTip>().Single().Card;
            Check(preview is WylderFireCoating && preview.IsUpgraded == up && preview.DynamicVars["Vigor"].BaseValue == (up ? 6 : 4),
                "Generated Fire Coating hover preview matches actual base/upgraded card");
        }
    }
    private static void LocalizedText()
    {
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter(), new ShowIfUpgradedFormatter());
        foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
        {
            using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
            string text = json.RootElement.GetProperty("WYLDER_FLAME_CHASE_SLASH.description").GetString()!;
            string Render(bool up, bool preview = false)
            {
                var card = Card<WylderFlameChaseSlash>(up);
                var vars = card.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value);
                vars["IfUpgraded"] = new IfUpgradedVar(preview ? UpgradeDisplay.UpgradePreview : up ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal);
                return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text, vars);
            }
            Check(!Render(false).Contains('+') && Render(true).Contains('+') && Render(true, true).Contains('+'), "Base/upgraded/preview generated-card naming");
            Check(json.RootElement.GetProperty("WYLDER_ASSAULT_STAKE.description").GetString()!.Contains(locale == "zhs" ? "绝境" : "Desperation"), "Desperation text present");
        }
    }
    private static void Check(bool ok, string reason) { _checks++; if (!ok) throw new Exception(reason); }
}
