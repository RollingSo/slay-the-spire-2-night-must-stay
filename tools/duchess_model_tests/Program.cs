using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Modding;
using NightMustStay.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Relics;
using System.Text.Json;

try
{
typeof(TestMode).GetProperty("IsOn")!.SetValue(null, true);
var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
typeof(ModManager).GetMethod("ResetForTests", flags)!.Invoke(null, null);
// Standalone model fixture deliberately skips workshop discovery and telemetry.
var modState = typeof(ModManager).GetProperty("State")!;
modState.SetValue(null, Enum.Parse(modState.PropertyType, "Skipped"));
var models = typeof(DuchessStrike).Assembly.GetTypes()
    .Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t)).ToArray();
var init = typeof(ModelDb).GetMethod("Init", flags)!;
init.Invoke(null, init.GetParameters().Length == 0 ? null : new object[] { models });
foreach (var type in models)
    if (!ModelDb.Contains(type)) typeof(ModelDb).GetMethod("Inject", flags)!.Invoke(null, new object[] { type });

var duchess = ModelDb.Character<Duchess>();
if (duchess.Id.Entry != "DUCHESS"
    || duchess.StartingHp != 66
    || duchess.StartingGold != 99
    || duchess.StartingRelics is not [DuchessOldPocketwatch])
{
    throw new Exception("Duchess character-select identity or starting loadout is incomplete.");
}

foreach (string locale in new[] { "zhs", "eng", "jpn" })
{
    string root = Path.GetFullPath(Path.Combine(
        Directory.GetCurrentDirectory(),
        "NightMustStay",
        "localization",
        locale));
    using JsonDocument characters = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(root, "characters.json")));
    using JsonDocument relics = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(root, "relics.json")));
    if (!characters.RootElement.TryGetProperty("DUCHESS.title", out _)
        || !characters.RootElement.TryGetProperty("DUCHESS.description", out _)
        || !relics.RootElement.TryGetProperty("DUCHESS_OLD_POCKETWATCH.title", out _)
        || !relics.RootElement.TryGetProperty("DUCHESS_OLD_POCKETWATCH.description", out _))
    {
        throw new Exception($"{locale} Duchess character-select localization is incomplete.");
    }
}

int count = 0;
foreach (var entry in DuchessCardCatalog.All)
{
    var type = typeof(DuchessStrike).Assembly.GetType("NightMustStay.Core.Models.Cards." + entry.Key)!;
    var canonical = (CardModel)typeof(ModelDb).GetMethod("Get", flags, null, new[] { typeof(Type) }, null)!.Invoke(null, new object[] { type })!;
    var card = canonical.ToMutable();
    foreach (var effect in entry.Value.Effects)
    {
        string key = effect.Kind switch
        {
            "AllyBlock" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 => "CalculationBase",
            _ => effect.Kind,
        };
        if (card.DynamicVars[key].BaseValue != effect.Amount)
            throw new Exception(entry.Key + " base variable " + key);
    }
    typeof(CardModel).GetMethod("UpgradeInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(card, null);
    foreach (var effect in entry.Value.Effects)
    {
        string key = effect.Kind switch
        {
            "AllyBlock" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 => "CalculationBase",
            _ => effect.Kind,
        };
        if (card.DynamicVars[key].BaseValue != effect.Upgraded)
            throw new Exception(entry.Key + " upgraded variable " + key);
    }
    count++;
}
Console.WriteLine($"PASS: instantiated and upgraded all {count} Duchess card models against installed game API.");
var starterTypes = duchess.StartingDeck.Select(card => card.GetType()).ToArray();
if (starterTypes.Count(type => type == typeof(DuchessStrike)) != 4
    || starterTypes.Count(type => type == typeof(DuchessDefend)) != 4
    || starterTypes.Count(type => type == typeof(DuchessElegantBearing)) != 1
    || starterTypes.Count(type => type == typeof(DuchessRestage)) != 1)
    throw new Exception("Duchess starter deck does not match 4 Strike, 4 Defend, Elegant Bearing, Restage.");

string[] removedEffects = { "Step", "Echo", "Veil", "AllyVeil", "OpeningDance", "MeasuredBreath", "RepriseGuard", "RepriseStep", "VeilReward", "Token" };
if (DuchessCardCatalog.All.Values.SelectMany(spec => spec.Effects).Any(effect => removedEffects.Contains(effect.Kind)))
    throw new Exception("A removed Duchess prototype mechanic remains in the card table.");

DuchessCardSpec restage = DuchessCardCatalog.All[nameof(DuchessRestage)];
DuchessCardSpec bearing = DuchessCardCatalog.All[nameof(DuchessElegantBearing)];
DuchessCardSpec dodge = DuchessCardCatalog.All[nameof(DuchessDodge)];
if (restage.Cost != 0 || restage.Type != CardType.Attack || !restage.UpgradeRetain || restage.TargetSelf
    || restage.Moment != 4 || restage.RestageDivisor != 3 || restage.Effects.Length != 1
    || restage.Effects[0].Kind != "Damage" || restage.Effects[0].Amount != 4)
    throw new Exception("Restage core specification is wrong.");
if (bearing.Cost != 1 || bearing.Effects[0].Kind != "DodgeToDraw"
    || bearing.Effects[0].Amount != 3 || !bearing.UpgradeTokens || bearing.Effects[1].Amount != 2)
    throw new Exception("Elegant Bearing core specification is wrong.");

var restageCard = ModelDb.Card<DuchessRestage>().ToMutable();
if (restageCard.TargetType != TargetType.AnyEnemy || restageCard.CanonicalKeywords.Contains(CardKeyword.Retain)
    || !restageCard.DynamicVars.ContainsKey("CalculationBase")
    || !restageCard.DynamicVars.ContainsKey("ExtraDamage")
    || !restageCard.DynamicVars.ContainsKey("CalculatedDamage"))
    throw new Exception("Dagger Restage must target an enemy and expose calculated damage preview variables.");
_ = restageCard.DynamicVars.CalculatedDamage.Calculate(null);
typeof(CardModel).GetMethod("UpgradeInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
    .Invoke(restageCard, null);
if (!restageCard.CanonicalKeywords.Contains(CardKeyword.Retain))
    throw new Exception("Restage upgrade preview must add Retain.");

if (DuchessCardCatalog.All.ContainsKey("DuchessDaggerRestage"))
    throw new Exception("The former standalone Dagger Restage card must be removed.");
var magicDagger = DuchessCardCatalog.All[nameof(DuchessMagicDagger)];
if (magicDagger.Cost != 0 || magicDagger.Moment != 0
    || magicDagger.Effects[0].Amount != 4 || magicDagger.Effects[0].Upgraded != 5
    || magicDagger.Effects[1].Amount != 2 || magicDagger.Effects[1].Upgraded != 3)
    throw new Exception("Magic Dagger specification is wrong.");
var returningCrosscut = DuchessCardCatalog.All[nameof(DuchessReturningCrosscut)];
if (returningCrosscut.Moment != 2 || returningCrosscut.Effects[0].Amount != 7
    || returningCrosscut.Effects[1].Kind != "Replay")
    throw new Exception("Returning Crosscut specification is wrong.");
var waitAMomentCard = ModelDb.Card<DuchessWaitAMoment>().ToMutable();
var foreseeFutureCard = ModelDb.Card<DuchessForeseeFuture>().ToMutable();
if (waitAMomentCard.DynamicVars["NextTurnEnergy"] is not EnergyVar
    || foreseeFutureCard.DynamicVars["FutureMomentEnergy"] is not EnergyVar)
    throw new Exception($"Duchess energy localization variables must use EnergyVar for energyIcons formatting: "
        + $"{waitAMomentCard.DynamicVars["NextTurnEnergy"].GetType().FullName}, "
        + foreseeFutureCard.DynamicVars["FutureMomentEnergy"].GetType().FullName);
var feint = DuchessCardCatalog.All[nameof(DuchessFeint)];
if (feint.Cost != 0 || feint.Type != CardType.Skill || !feint.UpgradeRetain
    || feint.Effects.Length != 2 || feint.Effects[0].Kind != "Draw"
    || feint.Effects[0].Amount != 1 || feint.Effects[1].Kind != "AdvanceMoment"
    || feint.Effects[1].Amount != 1)
    throw new Exception("Feint specification is wrong.");
var invitation = DuchessCardCatalog.All[nameof(DuchessInvitation)];
if (invitation.Effects.Length != 2 || invitation.Effects[0].Kind != "Block"
    || invitation.Effects[0].Amount != 9 || invitation.Effects[0].Upgraded != 12
    || invitation.Effects[1].Kind != "ReturnMoment" || invitation.Effects[1].Amount != 2)
    throw new Exception("Invitation specification is wrong.");
var grandReprise = DuchessCardCatalog.All[nameof(DuchessGrandReprise)];
if (grandReprise.Cost != 0 || !grandReprise.Exhaust || grandReprise.Effects.Length != 2
    || grandReprise.Effects[0].Kind != "ShuffleDiscardAll"
    || grandReprise.Effects[1].Amount != 3 || grandReprise.Effects[1].Upgraded != 3)
    throw new Exception("Grand Reprise specification is wrong.");

using (JsonDocument cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(
           Directory.GetCurrentDirectory(), "NightMustStay", "localization", "zhs", "cards.json"))))
{
    string baseText = cards.RootElement.GetProperty("DUCHESS_ELEGANT_BEARING.description").GetString()!;
    string upgradedText = cards.RootElement.GetProperty("DUCHESS_ELEGANT_BEARING.upgradeDescription").GetString()!;
    if (baseText.Contains("闪避+") || !upgradedText.Contains("闪避+"))
        throw new Exception("Elegant Bearing text must preview Dodge before upgrade and Dodge+ after upgrade.");
}
if (dodge.Cost != 1 || !dodge.Reaction || !dodge.Exhaust
    || dodge.Effects[0].Amount != 6 || dodge.Effects[0].Upgraded != 9 || dodge.Effects[1].Amount != 1)
    throw new Exception("Dodge core specification is wrong.");

if (DuchessCardCatalog.All[nameof(DuchessOpeningMoment)].Moment != 0
    || DuchessCardCatalog.All[nameof(DuchessReverseTime)].Moment != 12
    || !DuchessCardCatalog.All[nameof(DuchessCarianSlicer)].ShuffleSelf
    || DuchessCardCatalog.All[nameof(DuchessEphemeral)].UpgradedMoment != 5)
    throw new Exception("Duchess 0-12 Moment card specifications are incomplete.");

if (ModelDb.Power<DuchessMomentPower>().StackType
    != MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter)
    throw new Exception("Moment must use an independently stackable counter power.");

if (typeof(DuchessMomentPower).GetMethods(flags).Any(method => method.Name.Contains("Star", StringComparison.Ordinal)))
    throw new Exception("Moment must not reuse or mutate Regent Stars.");

var harmony = new HarmonyLib.Harmony("NightMustStay.Duchess.Tests");
foreach (var patch in typeof(NightMustStay.Core.Patches.DuchessMomentPatch).Assembly.GetTypes()
    .Where(t => t.Namespace == "NightMustStay.Core.Patches" && t.Name.StartsWith("Duchess")))
    harmony.CreateClassProcessor(patch).Patch();
harmony.UnpatchAll(harmony.Id);
Console.WriteLine("PASS: independent Moment, Reaction/Dodge core, starter loadout, and all Duchess patch bindings.");
return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
