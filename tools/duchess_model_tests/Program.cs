using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SmartFormat;
using SmartFormat.Extensions;
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
// The standalone fixture only discovers mod models; token cards also resolve
// the game's built-in token and colorless pools at runtime.
foreach (var type in new[] { typeof(TokenCardPool), typeof(ColorlessCardPool) })
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
    bool hasUpgrade = entry.Value.Effects.Any(effect => effect.Amount != effect.Upgraded)
        || entry.Value.UpgradeTokens || entry.Value.UpgradeX || entry.Value.UpgradeRetain
        || entry.Value.UpgradeInnate || entry.Value.UpgradeRemoveExhaust
        || entry.Value.UpgradedMoment >= 0 || entry.Value.UpgradeCost >= 0
        || entry.Value.UpgradeHits >= 0;
    if (card.IsUpgradable != hasUpgrade)
        throw new Exception(entry.Key + " offers a missing or no-op upgrade.");
    foreach (var effect in entry.Value.Effects)
    {
        string key = effect.Kind switch
        {
            "AllyBlock" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 || entry.Value.ConcealedTripleDamage
                || entry.Value.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage") => "CalculationBase",
            "ConcealedBonusDamage" or "MomentBonusDamage" => "ExtraDamage",
            "MomentDamage" => "CalculationBase",
            _ => effect.Kind,
        };
        if (card.DynamicVars[key].BaseValue != effect.Amount)
            throw new Exception(entry.Key + " base variable " + key);
    }
    if (hasUpgrade)
        typeof(CardModel).GetMethod("UpgradeInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(card, null);
    foreach (var effect in entry.Value.Effects)
    {
        string key = effect.Kind switch
        {
            "AllyBlock" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 || entry.Value.ConcealedTripleDamage
                || entry.Value.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage") => "CalculationBase",
            "ConcealedBonusDamage" or "MomentBonusDamage" => "ExtraDamage",
            "MomentDamage" => "CalculationBase",
            _ => effect.Kind,
        };
        if (card.DynamicVars[key].BaseValue != effect.Upgraded)
            throw new Exception(entry.Key + " upgraded variable " + key);
        if (card.DynamicVars[key].WasJustUpgraded != (effect.Amount != effect.Upgraded))
            throw new Exception(entry.Key + " highlights an unchanged number or misses a changed one: " + key);
    }
    count++;
}
Console.WriteLine($"PASS: instantiated and upgraded all {count} Duchess card models against installed game API.");
string[] tableIds = (
    "ElegantBearing BladeRevealMoment CarianSlicer MagicDagger ReturningCrosscut WaitAMoment OpeningMoment " +
    "Reenactment Ephemeral Overdraw ForeseeFuture BackToPast UndecidedFate ReverseTime Reveal SideSomersault " +
    "LightningNerves CarianSwordsmanship GrandReprise Feint SwayingStep Invitation PassingCut Initiative " +
    "PoisedExit GapMoonshadow MagicRadiantBlade RadiantBladeArray AngelWings CariaPhalanx GreatswordPhalanx " +
    "MiquellasHalo GoldenBlade CarianGreatsword CarianPiercer RadiantBladeMagic DeathBlade GlintstoneHail " +
    "CarianRetaliation FallingMagic Pivot Restage Reverberation ThreadTheGap QuickHands PerfectRehearsal " +
    "CalmComposure TidyCollar SoftLanding Distraction VeiledStep SilverFlash Beat Composure Silence " +
    "MidnightWaltz SilverStorm ThiefsArsenal Finale Duchess GlintstoneKnife HiddenPocket EternalRestage " +
    "GrandBearing GoldenMoment LorettaMastery LorettaGreatbow SleightOfHand BecomeInvisible BlindSpot")
    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
var expectedIds = tableIds.Select(id => "Duchess" + id)
    .Concat(new[] { nameof(DuchessStrike), nameof(DuchessDefend),
        nameof(DuchessRadiantBlade), nameof(DuchessDodge) }).ToHashSet();
if (tableIds.Length != 70 || !expectedIds.SetEquals(DuchessCardCatalog.All.Keys))
    throw new Exception("Duchess card IDs differ from the supplied 70-card table plus Strike, Defend, Dodge, and Radiant Blade.");
foreach (string name in new[] { nameof(DuchessRestage), nameof(DuchessGoldenBlade), nameof(DuchessSilverFlash), nameof(DuchessSilence) })
{
    var card = (DuchessCard)typeof(ModelDb).GetMethod("Get", flags, null, new[] { typeof(Type) }, null)!
        .Invoke(null, new object[] { typeof(DuchessStrike).Assembly.GetType("NightMustStay.Core.Models.Cards." + name)! })!;
    if (!card.DynamicVars.ContainsKey("CalculatedDamage"))
        throw new Exception(name + " must preview final calculated damage.");
    _ = card.ToMutable().DynamicVars.CalculatedDamage.Calculate(null);
}
if (!DuchessCardCatalog.All[nameof(DuchessSwayingStep)].UpgradeTokens
    || DuchessCardCatalog.All[nameof(DuchessSilverStorm)].UpgradeHits != 3)
    throw new Exception("Swaying Step token and Silver Storm hit-count upgrades are missing.");
foreach (string removed in new[] { "FollowThrough", "NoWitness", "Encore", "Backstage", "VanishingAct" })
{
    string name = "Duchess" + removed;
    if (DuchessCardCatalog.All.ContainsKey(name)
        || typeof(DuchessStrike).Assembly.GetType("NightMustStay.Core.Models.Cards." + name) != null)
        throw new Exception($"Removed Duchess card still exists: {name}");
}
var starterTypes = duchess.StartingDeck.Select(card => card.GetType()).ToArray();
if (starterTypes.Count(type => type == typeof(DuchessStrike)) != 4
    || starterTypes.Count(type => type == typeof(DuchessDefend)) != 4
    || starterTypes.Count(type => type == typeof(DuchessElegantBearing)) != 1
    || starterTypes.Count(type => type == typeof(DuchessBladeRevealMoment)) != 1
    || starterTypes.Contains(typeof(DuchessRestage)))
    throw new Exception("Duchess starter deck does not match 4 Strike, 4 Defend, Elegant Bearing, Blade Reveal Moment.");

string[] removedEffects = { "Step", "Echo", "Veil", "AllyVeil", "OpeningDance", "MeasuredBreath", "RepriseGuard", "RepriseStep", "VeilReward", "Token" };
if (DuchessCardCatalog.All.Values.SelectMany(spec => spec.Effects).Any(effect => removedEffects.Contains(effect.Kind)))
    throw new Exception("A removed Duchess prototype mechanic remains in the card table.");

DuchessCardSpec restage = DuchessCardCatalog.All[nameof(DuchessRestage)];
DuchessCardSpec bearing = DuchessCardCatalog.All[nameof(DuchessElegantBearing)];
DuchessCardSpec dodge = DuchessCardCatalog.All[nameof(DuchessDodge)];
if (restage.Cost != 0 || restage.Type != CardType.Attack || restage.Rarity != CardRarity.Uncommon || !restage.UpgradeRetain || restage.TargetSelf
    || restage.Moment != 6 || restage.RestageDivisor != 3 || restage.Effects.Length != 1
    || restage.Effects[0].Kind != "Damage" || restage.Effects[0].Amount != 4)
    throw new Exception("Restage core specification is wrong.");
if (bearing.Cost != 0 || bearing.Effects.Length != 1 || bearing.Effects[0].Kind != "DodgeToDraw"
    || bearing.Effects[0].Amount != 2 || !bearing.UpgradeTokens)
    throw new Exception("Elegant Bearing core specification is wrong.");
var bladeReveal = DuchessCardCatalog.All[nameof(DuchessBladeRevealMoment)];
if (bladeReveal.Cost != 1 || bladeReveal.Type != CardType.Attack || bladeReveal.Rarity != CardRarity.Basic
    || bladeReveal.Moment != 1 || bladeReveal.Effects.Length != 2
    || bladeReveal.Effects[0] != new DuchessEffect("Damage", 7, 10)
    || bladeReveal.Effects[1] != new DuchessEffect("Draw", 2, 2, "moment"))
    throw new Exception("Blade Reveal Moment specification is wrong.");

var passingCut = DuchessCardCatalog.All[nameof(DuchessPassingCut)];
if (passingCut.Cost != 2 || !passingCut.Reaction
    || passingCut.Effects[0].Kind != "Damage" || passingCut.Effects[0].Amount != 7 || passingCut.Effects[0].Upgraded != 9
    || passingCut.Effects[1].Kind != "Block" || passingCut.Effects[1].Amount != 7 || passingCut.Effects[1].Upgraded != 9)
    throw new Exception("Passing Cut specification is wrong.");

var blindSpot = DuchessCardCatalog.All[nameof(DuchessBlindSpot)];
if (blindSpot.Cost != 1 || blindSpot.Moment != -1 || blindSpot.Rarity != CardRarity.Uncommon
    || blindSpot.Effects[0].Kind != "Damage" || blindSpot.Effects[0].Amount != 8 || blindSpot.Effects[0].Upgraded != 11
    || blindSpot.Effects[1].Kind != "Vulnerable" || blindSpot.Effects[1].Amount != 2 || blindSpot.Effects[1].Upgraded != 3
    || blindSpot.Effects[1].Condition != "concealed")
    throw new Exception("Blind Spot specification is wrong.");

var poisedExit = DuchessCardCatalog.All[nameof(DuchessPoisedExit)];
if (poisedExit.Type != CardType.Skill || poisedExit.Cost != 1 || poisedExit.Moment != 3
    || poisedExit.Effects[0].Kind != "Block" || poisedExit.Effects[0].Amount != 7
    || poisedExit.Effects[1].Kind != "WeakAll" || poisedExit.Effects[1].Amount != 2 || poisedExit.Effects[1].Upgraded != 3)
    throw new Exception("Poised Exit specification is wrong.");
if (ModelDb.Card<DuchessPoisedExit>().ToMutable().TargetType != TargetType.Self)
    throw new Exception("Poised Exit must be playable without choosing an enemy.");

var pivot = DuchessCardCatalog.All[nameof(DuchessPivot)];
if (pivot.Cost != 0 || pivot.Type != CardType.Skill || pivot.Rarity != CardRarity.Common
    || pivot.Effects[0] != new DuchessEffect("Block", 4, 6)
    || pivot.Effects[1] != new DuchessEffect("AdvanceMoment", 1, 1))
    throw new Exception("Pivot specification is wrong.");
var reverberation = DuchessCardCatalog.All[nameof(DuchessReverberation)];
if (reverberation.Rarity != CardRarity.Uncommon || reverberation.Effects.Length != 2 || reverberation.UpgradeCost != 0
    || reverberation.Effects[0] != new DuchessEffect("ShuffleDiscard", 2, 2)
    || reverberation.Effects[1] != new DuchessEffect("Draw", 1, 1))
    throw new Exception("Reverberation specification is wrong.");
var composure = DuchessCardCatalog.All[nameof(DuchessComposure)];
if (composure.Effects.Length != 1 || composure.Effects[0] != new DuchessEffect("ReactionDrawBlock", 2, 3))
    throw new Exception("Composure must gain 2/3 Block when a Reaction card is drawn.");
var fallingMagic = DuchessCardCatalog.All[nameof(DuchessFallingMagic)];
if (fallingMagic.Type != CardType.Skill || fallingMagic.Rarity != CardRarity.Rare
    || !fallingMagic.Retain || fallingMagic.Effects.Length != 1
    || fallingMagic.Effects[0] != new DuchessEffect("ShuffleAoeDamage", 6, 9))
    throw new Exception("Falling Magic specification is wrong.");
if (DuchessCardCatalog.All[nameof(DuchessOpeningMoment)].Cost != 0)
    throw new Exception("Opening Moment must cost 0 Energy.");

foreach (var (name, expectedEffect) in new[]
{
    (nameof(DuchessLightningNerves), "ReactionDraw"),
    (nameof(DuchessCarianSwordsmanship), "TransformStrike"),
})
{
    var spec = DuchessCardCatalog.All[name];
    if (spec.Type != CardType.Power || spec.Rarity != CardRarity.Rare
        || spec.Cost != 2 || spec.UpgradeCost != 1
        || spec.Effects.Length != 1 || spec.Effects[0] != new DuchessEffect(expectedEffect, 1, 1))
        throw new Exception($"{name} specification is wrong.");
}
var swordsmanship = ModelDb.Card<DuchessCarianSwordsmanship>().ToMutable();
if (swordsmanship.TargetType != TargetType.Self || swordsmanship.EnergyCost.GetResolved() != 2)
    throw new Exception("Carian Swordsmanship must require no enemy target and cost 2.");
typeof(CardModel).GetMethod("UpgradeInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
    .Invoke(swordsmanship, null);
if (swordsmanship.EnergyCost.GetResolved() != 1)
    throw new Exception("Upgraded Carian Swordsmanship must cost 1.");
var carryUpgrade = typeof(DuchessCard).GetMethod("CarryStrikeUpgrade", BindingFlags.Static | BindingFlags.NonPublic)!;
foreach (bool upgradedStrike in new[] { false, true })
{
    var strike = ModelDb.Card<DuchessStrike>().ToMutable();
    var deckSlicer = ModelDb.Card<DuchessCarianSlicer>().ToMutable();
    var combatSlicer = ModelDb.Card<DuchessCarianSlicer>().ToMutable();
    if (upgradedStrike)
        typeof(CardModel).GetMethod("UpgradeInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(strike, null);
    carryUpgrade.Invoke(null, new object[] { strike, deckSlicer, combatSlicer });
    if (deckSlicer.IsUpgraded != upgradedStrike || combatSlicer.IsUpgraded != upgradedStrike)
        throw new Exception("Carian Swordsmanship must preserve the chosen Strike's upgrade in both deck and combat.");
}

var swayingStep = DuchessCardCatalog.All[nameof(DuchessSwayingStep)];
if (swayingStep.Type != CardType.Skill || swayingStep.Cost != 1 || swayingStep.Rarity != CardRarity.Uncommon
    || swayingStep.Effects.Length != 2 || swayingStep.Effects[0].Kind != "DodgeToDraw"
    || swayingStep.Effects[0].Amount != 3 || swayingStep.Effects[0].Upgraded != 3
    || swayingStep.Effects[1] != new DuchessEffect("Draw", 1, 1))
    throw new Exception("Swaying Step specification is wrong.");

var gapMoonshadow = DuchessCardCatalog.All[nameof(DuchessGapMoonshadow)];
if (gapMoonshadow.Type != CardType.Attack || gapMoonshadow.Rarity != CardRarity.Uncommon
    || gapMoonshadow.Cost != 1 || gapMoonshadow.Moment != 2 || gapMoonshadow.SecondaryMoment != 4
    || gapMoonshadow.Effects.Length != 3
    || gapMoonshadow.Effects[0] != new DuchessEffect("Damage", 7, 7)
    || gapMoonshadow.Effects[1] != new DuchessEffect("AoeDamage", 7, 9, "moment2")
    || gapMoonshadow.Effects[2] != new DuchessEffect("ExtraDamage", 14, 18, "moment4"))
    throw new Exception("Gap Moonshadow specification is wrong.");
var gapMoonshadowCard = ModelDb.Card<DuchessGapMoonshadow>().ToMutable();
if (gapMoonshadowCard.TargetType != TargetType.AnyEnemy
    || !gapMoonshadowCard.DynamicVars.ContainsKey("Damage")
    || !gapMoonshadowCard.DynamicVars.ContainsKey("AoeDamage")
    || !gapMoonshadowCard.DynamicVars.ContainsKey("ExtraDamage"))
    throw new Exception("Gap Moonshadow must expose all three damage previews and target an enemy.");

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
if (ModelDb.Power<DuchessTemporaryStrengthDownPower>().Type != MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff)
    throw new Exception("Magic Dagger's temporary Strength must be a positive buff, not immediate Strength loss.");
var returningCrosscut = DuchessCardCatalog.All[nameof(DuchessReturningCrosscut)];
if (returningCrosscut.Moment != 2 || returningCrosscut.Effects[0].Amount != 7
    || returningCrosscut.Effects[1].Kind != "Replay")
    throw new Exception("Returning Crosscut specification is wrong.");
var waitAMomentCard = ModelDb.Card<DuchessWaitAMoment>().ToMutable();
var foreseeFutureCard = ModelDb.Card<DuchessForeseeFuture>().ToMutable();
var waitAMoment = DuchessCardCatalog.All[nameof(DuchessWaitAMoment)];
if (waitAMoment.Effects.Length != 1 || waitAMoment.Effects[0].Kind != "NextTurnEnergyAndDraw"
    || waitAMoment.Effects[0].Amount != 1 || waitAMoment.Effects[0].Upgraded != 2
    || waitAMoment.Moment != 1 || waitAMoment.MomentCostReduction != 1)
    throw new Exception("Wait a Moment must grant 1 Energy and draw 1/2 cards next turn, with Moment 1 cost reduction.");
if (foreseeFutureCard.DynamicVars["FutureMomentEnergy"] is not EnergyVar)
    throw new Exception("Future Moment Energy localization must use EnergyVar for energyIcons formatting.");

var radiantBlade = DuchessCardCatalog.All[nameof(DuchessRadiantBlade)];
if (radiantBlade.Cost != 0 || radiantBlade.Type != CardType.Attack || radiantBlade.Rarity != CardRarity.Token
    || !radiantBlade.Exhaust || radiantBlade.Effects.Length != 2
    || radiantBlade.Effects[0] != new DuchessEffect("Damage", 4, 6)
    || radiantBlade.Effects[1] != new DuchessEffect("Draw", 1, 1))
    throw new Exception("Radiant Blade specification is wrong.");
var radiantBladeCard = ModelDb.Card<DuchessRadiantBlade>().ToMutable();
if (radiantBladeCard.Pool is not TokenCardPool || radiantBladeCard.VisualCardPool is not ColorlessCardPool)
    throw new Exception("Radiant Blade must be a colorless token card.");

var magicRadiantBlade = DuchessCardCatalog.All[nameof(DuchessMagicRadiantBlade)];
if (magicRadiantBlade.Cost != 0 || magicRadiantBlade.Type != CardType.Attack
    || magicRadiantBlade.Rarity != CardRarity.Uncommon || !magicRadiantBlade.XCost
    || !magicRadiantBlade.UpgradeX || !magicRadiantBlade.TargetSelf
    || magicRadiantBlade.Effects.Length != 1 || magicRadiantBlade.Effects[0].Kind != "RadiantBladeTurns")
    throw new Exception("Magic Radiant Blade specification is wrong.");

var radiantBladeArray = DuchessCardCatalog.All[nameof(DuchessRadiantBladeArray)];
if (radiantBladeArray.Cost != 1 || radiantBladeArray.Type != CardType.Attack
    || radiantBladeArray.Rarity != CardRarity.Common || !radiantBladeArray.TargetSelf
    || !radiantBladeArray.UpgradeTokens || radiantBladeArray.Effects.Length != 1
    || radiantBladeArray.Effects[0] != new DuchessEffect("RadiantBladeToDraw", 3, 3))
    throw new Exception("Radiant Blade Array specification is wrong.");
var cariaPhalanx = DuchessCardCatalog.All[nameof(DuchessCariaPhalanx)];
if (cariaPhalanx.Cost != 2 || cariaPhalanx.Type != CardType.Attack
    || cariaPhalanx.Rarity != CardRarity.Uncommon || !cariaPhalanx.Reaction
    || !cariaPhalanx.UpgradeTokens || !cariaPhalanx.TargetSelf
    || cariaPhalanx.Effects.Length != 1
    || cariaPhalanx.Effects[0] != new DuchessEffect("RadiantBladeToHand", 3, 3))
    throw new Exception("Caria Phalanx specification is wrong.");
var angelWings = DuchessCardCatalog.All[nameof(DuchessAngelWings)];
if (angelWings.Cost != 2 || angelWings.Type != CardType.Attack
    || angelWings.Rarity != CardRarity.Rare || angelWings.Effects.Length != 2
    || angelWings.Effects[0] != new DuchessEffect("Damage", 10, 10)
    || angelWings.Effects[1] != new DuchessEffect("ShuffleGrowth", 6, 9))
    throw new Exception("Angel Wings specification is wrong.");
var feint = DuchessCardCatalog.All[nameof(DuchessFeint)];
if (feint.Cost != 0 || feint.Type != CardType.Skill || !feint.UpgradeRetain
    || feint.Effects.Length != 2 || feint.Effects[0].Kind != "Draw"
    || feint.Effects[0].Amount != 1 || feint.Effects[1].Kind != "AdvanceMoment"
    || feint.Effects[1].Amount != 1)
    throw new Exception("Feint specification is wrong.");
var invitation = DuchessCardCatalog.All[nameof(DuchessInvitation)];
if (invitation.Cost != 0 || invitation.Type != CardType.Skill || !invitation.Exhaust
    || !invitation.UpgradeRemoveExhaust || !invitation.TargetSelf
    || invitation.Effects.Length != 2 || invitation.Effects[0].Kind != "ChooseDrawToTop"
    || invitation.Effects[1].Kind != "SetMoment" || invitation.Effects[1].Amount != 0)
    throw new Exception("Invitation specification is wrong.");
var threadTheGap = DuchessCardCatalog.All[nameof(DuchessThreadTheGap)];
if (threadTheGap.Cost != 2 || threadTheGap.Type != CardType.Power
    || threadTheGap.UpgradeCost != 1 || threadTheGap.Effects.Length != 1
    || threadTheGap.Effects[0].Kind != "EndTurnMomentBlock")
    throw new Exception("Thread the Gap specification is wrong.");
var glintstoneHail = DuchessCardCatalog.All[nameof(DuchessGlintstoneHail)];
if (glintstoneHail.Cost != 0 || glintstoneHail.Type != CardType.Attack
    || glintstoneHail.Rarity != CardRarity.Uncommon || glintstoneHail.Moment != 5
    || glintstoneHail.Effects.Length != 2
    || glintstoneHail.Effects[0] != new DuchessEffect("Damage", 4, 4)
    || glintstoneHail.Effects[1] != new DuchessEffect("ReturnHandDamageBoost", 6, 9, "moment"))
    throw new Exception("Glintstone Hail specification is wrong.");
if (DuchessCardCatalog.All.ContainsKey("DuchessSecondThought"))
    throw new Exception("Second Thought must be completely removed from the card catalog.");
foreach (string removed in new[] { "SilkenGuard", "TripleWaltz", "DancePartner", "MoonlitVeil", "DressRehearsal", "Elegance", "Afterglow" })
    if (DuchessCardCatalog.All.ContainsKey("Duchess" + removed))
        throw new Exception($"{removed} must be removed from the Duchess catalog.");
var switcheroo = DuchessCardCatalog.All[nameof(DuchessGlintstoneKnife)];
if (switcheroo.Type != CardType.Power || switcheroo.Rarity != CardRarity.Rare
    || switcheroo.Cost != 1 || switcheroo.UpgradeCost != 0
    || switcheroo.Effects.Length != 1 || switcheroo.Effects[0].Kind != "TurnStartSwap")
    throw new Exception("Switcheroo specification is wrong.");
var pocket = DuchessCardCatalog.All[nameof(DuchessHiddenPocket)];
if (pocket.Type != CardType.Skill || pocket.Rarity != CardRarity.Uncommon
    || pocket.Cost != 1 || pocket.Effects.Length != 1
    || pocket.Effects[0] != new DuchessEffect("HandToDrawTopBlock", 3, 4))
    throw new Exception("Hidden Pocket specification is wrong.");
var finale = DuchessCardCatalog.All[nameof(DuchessFinale)];
if (finale.Type != CardType.Skill || finale.Rarity != CardRarity.Rare
    || finale.Cost != 2 || finale.UpgradeCost != 1 || !finale.Exhaust
    || finale.Effects.Length != 1 || finale.Effects[0] != new DuchessEffect("AllyIntangible", 1, 1))
    throw new Exception("Final Curtain specification is wrong.");
var arsenal = DuchessCardCatalog.All[nameof(DuchessThiefsArsenal)];
if (arsenal.Cost != 1 || arsenal.UpgradeCost != 0 || !arsenal.Exhaust
    || arsenal.Effects.Length != 1 || arsenal.Effects[0].Kind != "DrawUntilReaction")
    throw new Exception("Thief's Arsenal specification is wrong.");
var waltz = DuchessCardCatalog.All[nameof(DuchessMidnightWaltz)];
if (waltz.Cost != 0 || waltz.Moment != 12 || !waltz.All
    || waltz.Effects.Length != 1 || waltz.Effects[0] != new DuchessEffect("Damage", 60, 75, "moment"))
    throw new Exception("Midnight Waltz specification is wrong.");
var grandReprise = DuchessCardCatalog.All[nameof(DuchessGrandReprise)];
if (grandReprise.Cost != 0 || !grandReprise.Exhaust || grandReprise.Effects.Length != 2
    || grandReprise.Effects[0].Kind != "ShuffleDiscardAll"
    || grandReprise.Effects[1].Amount != 3 || grandReprise.Effects[1].Upgraded != 4)
    throw new Exception("Grand Reprise specification is wrong.");

using (JsonDocument cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(
           Directory.GetCurrentDirectory(), "NightMustStay", "localization", "zhs", "cards.json"))))
{
    string baseText = cards.RootElement.GetProperty("DUCHESS_ELEGANT_BEARING.description").GetString()!;
    string upgradedText = cards.RootElement.GetProperty("DUCHESS_ELEGANT_BEARING.upgradeDescription").GetString()!;
    if (baseText.Contains("闪避+") || !upgradedText.Contains("闪避+"))
        throw new Exception("Elegant Bearing text must preview Dodge before upgrade and Dodge+ after upgrade.");
    var formatter = new SmartFormatter();
    formatter.AddExtensions(new DictionarySource(), new ReflectionSource(), new DefaultSource());
    formatter.AddExtensions(new HighlightDifferencesFormatter(), new ShowIfUpgradedFormatter());
    var bearingPreviewCard = ModelDb.Card<DuchessElegantBearing>().ToMutable();
    string renderedBase = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, baseText,
        new Dictionary<string, object> { ["DodgeToDraw"] = bearingPreviewCard.DynamicVars["DodgeToDraw"], ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.Normal) });
    string renderedPreview = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, baseText,
        new Dictionary<string, object> { ["DodgeToDraw"] = bearingPreviewCard.DynamicVars["DodgeToDraw"], ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.UpgradePreview) });
    if (renderedBase.Contains("闪避+") || !renderedPreview.Contains("闪避[green]+[/green]"))
        throw new Exception("The game's formatter must visibly change Elegant Bearing's generated Dodge on upgrade preview.");
    string haloText = cards.RootElement.GetProperty("DUCHESS_MIQUELLAS_HALO.description").GetString()!;
    if (!haloText.Contains("造成等同于当前[gold]时刻[/gold]的伤害({CalculatedDamage:diff()}点)。")
        || !haloText.Contains("将这张牌放到[gold]抽牌堆[/gold]顶部。"))
        throw new Exception("Miquella's Halo must describe Moment damage and bind its battle preview to CalculatedDamage.");
    var halo = ModelDb.Card<DuchessMiquellasHalo>().ToMutable();
    string haloBase = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, haloText,
        new Dictionary<string, object> { ["CalculatedDamage"] = halo.DynamicVars.CalculatedDamage, ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.Normal) });
    string haloPreview = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, haloText,
        new Dictionary<string, object> { ["CalculatedDamage"] = halo.DynamicVars.CalculatedDamage, ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.UpgradePreview) });
    if (haloBase.Contains("升级后耗能") || haloPreview.Contains("升级后耗能") || haloBase != haloPreview
        || DuchessCardCatalog.All[nameof(DuchessMiquellasHalo)].UpgradeCost != 0)
        throw new Exception("Cost-only upgrades must change the energy badge without redundant rules text.");
    string phalanxBase = cards.RootElement.GetProperty("DUCHESS_CARIA_PHALANX.description").GetString()!;
    string phalanxUpgrade = cards.RootElement.GetProperty("DUCHESS_CARIA_PHALANX.upgradeDescription").GetString()!;
    if (!phalanxBase.Contains("辉剑") || phalanxBase.Contains("辉剑+") || !phalanxUpgrade.Contains("辉剑+"))
        throw new Exception("Caria Phalanx must preview Radiant Blade before upgrade and Radiant Blade+ after upgrade.");
    string angelBase = cards.RootElement.GetProperty("DUCHESS_ANGEL_WINGS.description").GetString()!;
    string angelUpgrade = cards.RootElement.GetProperty("DUCHESS_ANGEL_WINGS.upgradeDescription").GetString()!;
    if (!angelBase.Contains("伤害+{ShuffleGrowth:diff()}")
        || !angelUpgrade.Contains("伤害+{ShuffleGrowth:diff()}"))
        throw new Exception("Angel Wings must bind its visible shuffle-growth amount to the upgraded dynamic variable.");
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
if (DuchessMomentPower.PocketwatchMoment != 2
    || DuchessConcealmentPower.CardBlockMultiplier != 1.25m
    || DuchessConcealmentPower.AttackDamageMultiplier != 1.25m)
    throw new Exception("Pocketwatch must trigger at Moment 2 and Concealment must add 25% attack damage and card Block.");
if (typeof(DuchessConcealmentPower).GetMethod("ModifyDamageMultiplicative")?.DeclaringType != typeof(DuchessConcealmentPower))
    throw new Exception("Concealment must modify powered attack damage.");
if (typeof(DuchessConcealmentPower).GetMethod("BeforeCardPlayed")?.DeclaringType == typeof(DuchessConcealmentPower)
    || typeof(DuchessConcealmentPower).GetMethod("AfterCardPlayedLate")?.DeclaringType == typeof(DuchessConcealmentPower)
    || typeof(DuchessConcealmentPower).GetMethod("BeforeSideTurnEnd")?.DeclaringType != typeof(DuchessConcealmentPower))
    throw new Exception("Concealment must lose one stack at the end of its owner's turn, not after card plays.");
if (typeof(DuchessFullBlockRadiantBladePower).GetMethod("AfterDamageReceived")?.DeclaringType == typeof(DuchessFullBlockRadiantBladePower)
    || typeof(DuchessFullBlockRadiantBladePower).GetMethod("AfterFullyBlockedAttack")?.DeclaringType != typeof(DuchessFullBlockRadiantBladePower))
    throw new Exception("Carian Retaliation must resolve once after the entire attack, not once per hit.");
var filterOrder = (string[])typeof(NightMustStay.Core.Patches.DuchessLibraryPatch)
    .GetField("ModFilterOrder", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
if (!filterOrder.SequenceEqual(new[] { "GuardianPool", "IroneyePool", "RevenantPool", "DuchessPool" }))
    throw new Exception("The four mod characters must occupy the first four card-library filters in order.");

if (typeof(DuchessMomentPower).GetMethods(flags).Any(method => method.Name.Contains("Star", StringComparison.Ordinal)))
    throw new Exception("Moment must not reuse or mutate Regent Stars.");

if (DuchessReactionRules.IsEligibleDraw(true, PileType.Hand)
    || !DuchessReactionRules.IsEligibleDraw(false, PileType.Hand)
    || DuchessReactionRules.IsEligibleDraw(false, PileType.Draw))
    throw new Exception("Reaction must exclude only the native opening hand draw, not other draws.");
foreach (Type type in new[] { typeof(DuchessCard), typeof(DuchessReactionDrawPower),
             typeof(DuchessReactionDrawBlockPower), typeof(DuchessSilverThimble) })
{
    if (type.GetMethod("AfterCardDrawn")?.DeclaringType != type)
        throw new Exception($"{type.Name} must use the native draw hook for Reaction effects.");
}

var harmony = new HarmonyLib.Harmony("NightMustStay.Duchess.Tests");
foreach (var patch in typeof(NightMustStay.Core.Patches.DuchessMomentPatch).Assembly.GetTypes()
    .Where(t => t.Namespace == "NightMustStay.Core.Patches" && t.Name.StartsWith("Duchess")))
    harmony.CreateClassProcessor(patch).Patch();
var activate = HarmonyLib.AccessTools.Method(typeof(NCombatUi), nameof(NCombatUi.Activate));
var momentCounterPatch = typeof(NightMustStay.Core.Patches.DuchessAssetPatch)
    .GetMethod(nameof(NightMustStay.Core.Patches.DuchessAssetPatch.InitializeDuchessMomentCounter))!;
if (HarmonyLib.Harmony.GetPatchInfo(activate)?.Postfixes.Any(p => p.PatchMethod == momentCounterPatch) != true)
    throw new Exception("Duchess Moment UI must be attached after combat UI reparents the star counter.");
harmony.UnpatchAll(harmony.Id);
Console.WriteLine("PASS: independent Moment, Reaction/Dodge core, starter loadout, and all Duchess patch bindings.");
return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
