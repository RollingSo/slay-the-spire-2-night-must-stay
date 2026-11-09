using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
using SmartFormat;
using SmartFormat.Extensions;

// Keep actual AttackCommand.Execute/MoveNext, hit-limit patch, Results/history,
// BeforeAttack/AfterAttack, X resolution and card callbacks. Only animation
// and damage UI/HP application are replaced; modifiers/Break remain real.
internal static class WylderEochaidRegression
{
    private static readonly Type Shared = typeof(WylderBattleArtRegression);
    private static Player Owner => (Player)AccessTools.Field(Shared, "_owner").GetValue(null)!;
    private static CombatState Combat => (CombatState)AccessTools.Field(Shared, "_combat").GetValue(null)!;
    private static Creature[] Enemies => (Creature[])AccessTools.Field(Shared, "_enemies").GetValue(null)!;
    private static readonly List<int> DamageValues = new();
    private static readonly Queue<int> ForcedDamage = new();
    private static bool _blocked, _killAttacker;
    private static int _checks, _xBonus;

    internal static void Run()
    {
        var h = new HarmonyLib.Harmony("NightMustStay.Wylder.Eochaid.Regression");
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderEochaidHitBudgetPatch")!).Patch();
        h.Patch(typeof(CreatureCmd).GetMethods().Single(m => m.Name == "Damage" && m.GetParameters().Length >= 6
            && m.GetParameters()[1].ParameterType == typeof(IEnumerable<Creature>) && m.GetParameters()[2].ParameterType == typeof(decimal)),
            prefix: new HarmonyMethod(typeof(WylderEochaidRegression), nameof(Damage)));
        h.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim)),
            prefix: new HarmonyMethod(typeof(WylderEochaidRegression), nameof(Animation)));
        h.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifyXValue)),
            postfix: new HarmonyMethod(typeof(WylderEochaidRegression), nameof(XBonus)));
        try
        {
            foreach (bool up in new[] { false, true })
            foreach (bool pursuit in new[] { false, true })
            foreach (bool blocked in new[] { false, true })
            foreach (int x in new[] { 0, 1, 2, 3, 4 })
            {
                Fixture(pursuit); _blocked = blocked;
                var card = Card(up, x);
                Check(card.EnergyCost.CostsX && card.Type == CardType.Attack && card.Rarity == CardRarity.Rare
                    && card.TargetType == TargetType.AnyEnemy, "Native X-cost Rare single-target attack");
                Check(card.DynamicVars.Damage.BaseValue == 9 && card.DynamicVars["Break"].BaseValue == 20
                    && card.DynamicVars.Repeat.BaseValue == (up ? 1 : 0), "Fixed9/Break20 and upgraded extra1");
                Check(!card.DynamicVars.Damage.WasJustUpgraded && !card.DynamicVars["Break"].WasJustUpgraded
                    && card.DynamicVars.Repeat.WasJustUpgraded == up, "Only X bonus changes on upgrade");
                Play(card);
                int initial = x + (up ? 1 : 0), hitDamage = pursuit ? 13 : 9;
                int expected = initial;
                Check(DamageValues.Count == expected && DamageValues.All(v => v == hitDamage),
                    $"Actual native loop: X{x}, up{up}, pursuit{pursuit}, blocked{blocked} -> {expected} hits");
                Check(!Owner.Creature.HasPower<WylderPursuitPower>(), "Pursuit exits after entire attack");
            }
            foreach (int[] firstHits in new[] { new[] { 20, 19 }, new[] { 20, 20 }, new[] { 0, 0 } })
            {
                Fixture(); foreach (int n in firstHits) ForcedDamage.Enqueue(n);
                Play(Card(false, 2));
                Check(DamageValues.Count == (firstHits.All(n => n >= 20) ? 4 : 2), "Every original hit must independently reach Break20");
            }
            Fixture(); ForcedDamage.Enqueue(20); ForcedDamage.Enqueue(20); ForcedDamage.Enqueue(20);
            Play(Card(false, 3));
            Check(DamageValues.Count == 6, "Break on the final original hit still grants the full extra hit budget");
            Fixture(true);
            AccessTools.Method(Shared, "AddPower").MakeGenericMethod(typeof(VigorPower)).Invoke(null, new object[] { Owner.Creature, 2 });
            var vigor = Card(false, 2); Play(vigor);
            Check(DamageValues.SequenceEqual(new[] { 16, 16 }) && !Owner.Creature.HasPower<VigorPower>(),
                "Vigor and Pursuit remain active through all original hits and clear at attack/play end");
            Fixture(); _xBonus = 2;
            Play(Card(false, 1));
            Check(DamageValues.Count == 3, "ResolveEnergyXValue honors engine X modifiers without summing sub-threshold hits");
            Fixture(); var replay = Card(false, 3); Play(replay); DamageValues.Clear(); Play(replay);
            Check(DamageValues.Count == 3, "A later play of the same copy resets the per-hit Break ledger");
            Fixture(); var a = Card(false, 3); Play(a); DamageValues.Clear(); Play(Card(false, 2));
            Check(DamageValues.Count == 2, "Other copies do not inherit successful Break");
            Fixture(); _killAttacker = true; Play(Card(false, 4));
            Check(DamageValues.Count == 1, "Native attacker-death exit retained");
            Fixture(); Enemies[0].SetCurrentHpInternal(15); Play(Card(false, 4));
            Check(DamageValues.Count == 2 && Enemies[0].IsDead, "No damage is dealt to a dead target for remaining/bonus hits");
            foreach(bool up in new[]{false,true})foreach(int firstDamage in new[]{0,19,20,21})foreach(bool blocked in new[]{false,true})
            {
                Fixture();_blocked=blocked;ForcedDamage.Enqueue(firstDamage);
                var vortex=(WylderOrdovisVortex)AccessTools.Method(Shared,"Card").Invoke(null,new object[]{ModelDb.Card<WylderOrdovisVortex>(),up})!;
                Play(vortex);Check(DamageValues.Count==(firstDamage>=20?3:1),"Ordovis native loop exact19/20 blocked boundary");
                DamageValues.Clear();Play(vortex);Check(DamageValues.Count==1,"Ordovis later play resets added hit budget");
            }
            Fixture();Enemies[0].SetCurrentHpInternal(1);ForcedDamage.Enqueue(20);
            var lethalVortex=(WylderOrdovisVortex)AccessTools.Method(Shared,"Card").Invoke(null,new object[]{ModelDb.Card<WylderOrdovisVortex>(),false})!;
            Play(lethalVortex);Check(DamageValues.Count==1,"Ordovis stops added hits after target death");
            LocalizedText();
            Check(File.Exists(ModelDb.Card<WylderEochaidsDancingBlade>().PortraitPath.Replace("res://", "")), "Custom portrait resolves");
            Console.WriteLine($"PASS: {_checks} Eochaid checks using the real native attack loop: every original hit must reach Break20, with upgrades, Pursuit, full block, last-hit doubling, Vigor, X modifiers, reset/copy isolation and death exits.");
        }
        finally { h.UnpatchAll(h.Id); }
    }
    private static void Fixture(bool pursuit = false)
    {
        AccessTools.Method(Shared, "Fixture").Invoke(null, new object[] { pursuit });
        DamageValues.Clear(); ForcedDamage.Clear(); _blocked = false; _killAttacker = false; _xBonus = 0;
    }
    private static WylderEochaidsDancingBlade Card(bool up, int x)
    {
        var card = (WylderEochaidsDancingBlade)AccessTools.Method(Shared, "Card").Invoke(null,
            new object[] { ModelDb.Card<WylderEochaidsDancingBlade>(), up })!;
        card.EnergyCost.CapturedXValue = x; return card;
    }
    private static void Play(CardModel card) => AccessTools.Method(Shared, "Play").Invoke(null,
        new object?[] { card, true, false, 0, null, null });
    public static bool Animation(ref Task __result) { __result = Task.CompletedTask; return false; }
    public static void XBonus(CardModel card, ref int __result) { if (card is WylderEochaidsDancingBlade) __result += _xBonus; }
    public static bool Damage(IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature dealer,
        CardModel? cardSource, ref Task<IEnumerable<DamageResult>> __result)
    {
        if (cardSource is not WylderEochaidsDancingBlade and not WylderOrdovisVortex) return true;
        __result = ResolveDamage(targets, props, dealer, cardSource); return false;
    }
    private static async Task<IEnumerable<DamageResult>> ResolveDamage(IEnumerable<Creature> targets, ValueProp props, Creature dealer, CardModel card)
    {
        var results = new List<DamageResult>();
        foreach (Creature target in targets.Where(target => target.IsAlive).ToArray())
        {
            card.DynamicVars.Damage.UpdateCardPreview(card, CardPreviewMode.Normal, target, true);
            int damage = ForcedDamage.Count > 0 ? ForcedDamage.Dequeue() : (int)Math.Floor(card.DynamicVars.Damage.PreviewValue);
            DamageValues.Add(damage);
            var result = new DamageResult(target, props) { UnblockedDamage = _blocked ? 0 : damage, BlockedDamage = _blocked ? damage : 0 };
            if (!_blocked) target.SetCurrentHpInternal(Math.Max(0, target.CurrentHp - damage));
            await card.AfterDamageGiven(null!, dealer, result, props, target, card);
            results.Add(result);
            if (_killAttacker) dealer.SetCurrentHpInternal(0);
        }
        return results;
    }
    private static void LocalizedText()
    {
        var formatter = new SmartFormatter();
        formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
        formatter.AddExtensions(new HighlightDifferencesFormatter(), new ShowIfUpgradedFormatter());
        foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
        {
            using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
            string key = "WYLDER_EOCHAIDS_DANCING_BLADE";
            string text = json.RootElement.GetProperty(key + ".description").GetString()!;
            Check(text == json.RootElement.GetProperty(key + ".upgradeDescription").GetString(), "Upgrade audit copy synchronized");
            string Render(bool up, bool preview = false)
            {
                var card = Card(up, 3);
                var vars = card.DynamicVars.ToDictionary(p => p.Key, p => (object)p.Value);
                // CardModel.GetDescriptionForPile injects this contextual var;
                // it is not one of the card's canonical balance variables.
                vars["IfUpgraded"] = new IfUpgradedVar(preview ? UpgradeDisplay.UpgradePreview
                    : up ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal);
                return formatter.Format(System.Globalization.CultureInfo.InvariantCulture, text, vars);
            }
            string basic = Render(false), upgraded = Render(true), preview = Render(true, true);
            Check(basic != upgraded && !basic.Contains('{') && !upgraded.Contains('{') && upgraded.Contains("X+"), "Runtime X vs X+1 text renders visibly different");
            Check(preview.Contains("X+") && preview.Contains("[green]") && !preview.Contains('{'), "Upgrade preview displays X+1 change");
            Check(text.Contains("[gold]" + (locale == "zhs" ? "破势" : "Break") + "[/gold]"), "Break keyword gold");
        }
    }
    private static void Check(bool ok, string reason) { _checks++; if (!ok) throw new Exception("Eochaid: " + reason); }
}
