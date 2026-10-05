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
using NightMustStay.Core.Models.Potions;
using NightMustStay.Core.Patches;
using System.Text.Json;
using NightMustStay.Core.Models.Revenant;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using System.Text.Json.Serialization;

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
if (args.Contains("--necro-hive-only"))
{
    NecroHiveRegression.Run();
    return 0;
}
if (args.Contains("--three-fingers-only"))
{
    FrenziedThreeFingersRegression.Run();
    return 0;
}
FortifyRetentionRegression.Run();
RevenantRoutingRegression.Run();
var wingsBalance = ModelDb.Card<WorldEndingWings>().ToMutable();
if (wingsBalance.DynamicVars.Damage.BaseValue != 7 || wingsBalance.TargetType != TargetType.AllEnemies)
    throw new Exception("World Ending Wings must use 7-damage native AOE hits.");
wingsBalance.UpgradeInternal();
if (wingsBalance.DynamicVars.Damage.BaseValue != 9) throw new Exception("World Ending Wings upgrade must deal 9 per exhausted Skill.");
var retreatBalance = (RetreatingDefense)ModelDb.Card<RetreatingDefense>().ToMutable();
if (retreatBalance.DynamicVars.Block.BaseValue != 4 || !retreatBalance.Keywords.Contains(CardKeyword.Exhaust))
    throw new Exception("Retreating Defense must give 4 Block and exhaust before upgrade.");
retreatBalance.DynamicVars["BlockedAttackDamage"].UpdateCardPreview(retreatBalance, CardPreviewMode.Normal, null!, false);
if (retreatBalance.BlockedAttackDamageThisTurn() != 0 || retreatBalance.DynamicVars["BlockedAttackDamage"].PreviewValue != 0)
    throw new Exception("Retreating Defense compendium preview must not read absent combat history.");
retreatBalance.UpgradeInternal();
if (retreatBalance.Keywords.Contains(CardKeyword.Exhaust) || retreatBalance.DynamicVars.Block.BaseValue != 4
    || retreatBalance.DynamicVars.Block.WasJustUpgraded)
    throw new Exception("Retreating Defense upgrade must only remove Exhaust.");
var retreatPlayer = (Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Player));
var retreatCreature = new Creature(retreatPlayer, 70, 70);
typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(retreatPlayer, retreatCreature);
var retreatState = new MegaCrit.Sts2.Core.Combat.CombatState();
retreatCreature.CombatState = retreatState;
retreatBalance.Owner = retreatPlayer;
var retreatHistory = MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History;
var retreatEntries = (List<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry>)retreatHistory.GetType()
    .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(retreatHistory)!;
var addedRetreatEntries = new List<MegaCrit.Sts2.Core.Combat.History.CombatHistoryEntry>();
void AddRetreatDamage(int blocked, ValueProp props, int round, MegaCrit.Sts2.Core.Combat.CombatSide side)
{
    var entry = new MegaCrit.Sts2.Core.Combat.History.Entries.DamageReceivedEntry(
        new DamageResult(retreatCreature, props) { BlockedDamage = blocked, UnblockedDamage = 5 },
        retreatCreature, null, null, round, side, retreatHistory, Array.Empty<Player>());
    retreatEntries.Add(entry);
    addedRetreatEntries.Add(entry);
}
try
{
    if (retreatBalance.BlockedAttackDamageThisTurn() != 0) throw new Exception("Empty turn must have zero blocked attack damage.");
    AddRetreatDamage(3, ValueProp.Move, retreatState.RoundNumber, retreatState.CurrentSide);
    if (retreatBalance.BlockedAttackDamageThisTurn() != 3) throw new Exception("Partial blocks must count their blocked portion only.");
    AddRetreatDamage(4, ValueProp.Move, retreatState.RoundNumber, retreatState.CurrentSide);
    AddRetreatDamage(99, ValueProp.Unpowered, retreatState.RoundNumber, retreatState.CurrentSide);
    AddRetreatDamage(99, ValueProp.Move, retreatState.RoundNumber - 1, retreatState.CurrentSide);
    AddRetreatDamage(99, ValueProp.Move, retreatState.RoundNumber, MegaCrit.Sts2.Core.Combat.CombatSide.Enemy);
    retreatBalance.DynamicVars["BlockedAttackDamage"].UpdateCardPreview(retreatBalance, CardPreviewMode.Normal, null!, false);
    if (retreatBalance.BlockedAttackDamageThisTurn() != 7 || retreatBalance.DynamicVars["BlockedAttackDamage"].PreviewValue != 7)
        throw new Exception("Blocked damage preview must sum current-turn attack blocks, excluding unpowered and older-turn damage.");
}
finally
{
    foreach (var entry in addedRetreatEntries) retreatEntries.Remove(entry);
    retreatCreature.CombatState = null;
}
var probingBalance = ModelDb.Card<ProbingStab>().ToMutable();
if (probingBalance.DynamicVars.Damage.BaseValue != 6 || probingBalance.DynamicVars["RetainCount"].IntValue != 2)
    throw new Exception("Probing Stab must deal 6 and retain up to 2 at end of turn.");
probingBalance.UpgradeInternal();
if (probingBalance.DynamicVars.Damage.BaseValue != 8 || probingBalance.DynamicVars["RetainCount"].IntValue != 3)
    throw new Exception("Probing Stab upgrade must deal 8 and retain up to 3.");
Console.WriteLine("PASS: World Ending Wings 7/9 AOE, Retreating Defense 4 Block and Exhaust removal, Probing Stab 6/8 and 2/3 retain.");
var seeThroughBalance = ModelDb.Card<SeeThrough>().ToMutable();
var volleyBalance = (SoulChasingVolley)ModelDb.Card<SoulChasingVolley>().ToMutable();
var halberdBalance = ModelDb.Card<ThousandWeightHalberd>().ToMutable();
var deadFireBalance = ModelDb.Card<DeadRealmSpiritFire>().ToMutable();
foreach (var xCard in new[] { seeThroughBalance, volleyBalance, halberdBalance, deadFireBalance })
    if (!xCard.EnergyCost.CostsX) throw new Exception($"{xCard.Id} must cost X.");
if (seeThroughBalance.DynamicVars.Block.BaseValue != 4
    || volleyBalance.DynamicVars.CalculatedDamage.Calculate(null) != 5
    || volleyBalance.DynamicVars.Repeat.IntValue != 1
    || halberdBalance.Type != CardType.Attack || halberdBalance.TargetType != TargetType.AnyEnemy
    || halberdBalance.DynamicVars.Damage.BaseValue != 8
    || deadFireBalance.DynamicVars.Damage.BaseValue != 1 || deadFireBalance.TargetType != TargetType.AllEnemies)
    throw new Exception("Revised X-card values, native calculation or targeting disagree.");
volleyBalance.UpgradeInternal();
if (volleyBalance.DynamicVars.Repeat.IntValue != 2 || volleyBalance.DynamicVars.CalculatedDamage.Calculate(null) != 5)
    throw new Exception("Volley upgrade must change X+1 hits to X+2, not base damage.");
var volleyMultiplier = (Func<CardModel, Creature?, decimal>)typeof(CalculatedVar)
    .GetField("_multiplierCalc", BindingFlags.Instance | BindingFlags.NonPublic)!
    .GetValue(volleyBalance.DynamicVars.CalculatedDamage)!;
var volleyActive = typeof(SoulChasingVolley).GetField("_resolvingVolley", BindingFlags.Instance | BindingFlags.NonPublic)!;
volleyBalance.OnMarkTriggered(5);
if (volleyMultiplier(volleyBalance, null) != 0) throw new Exception("Mark triggers outside the volley must not increase it.");
volleyActive.SetValue(volleyBalance, true);
foreach (int triggerCount in new[] { 0, 1, 2 })
{
    if (triggerCount > 0) volleyBalance.OnMarkTriggered(5);
    decimal damage = volleyBalance.DynamicVars.CalculationBase.BaseValue
        + volleyBalance.DynamicVars.ExtraDamage.BaseValue * volleyMultiplier(volleyBalance, null);
    if (damage != 5 + 5 * triggerCount) throw new Exception("Volley must accumulate 5/10/15 damage after zero/one/two Mark triggers.");
}
volleyActive.SetValue(volleyBalance, false);
var featherBalance = ModelDb.Card<Featherstep>().ToMutable();
if (featherBalance.EnergyCost.Canonical != 2 || featherBalance.DynamicVars.Cards.IntValue != 1)
    throw new Exception("Featherstep must cost 2 and draw 1.");
featherBalance.UpgradeInternal();
if (featherBalance.EnergyCost.GetResolved() != 1 || featherBalance.DynamicVars.Cards.IntValue != 1)
    throw new Exception("Featherstep upgrade must only reduce cost.");
var packBalance = ModelDb.Card<PackUp>().ToMutable();
if (packBalance.DynamicVars.Cards.IntValue != 2) throw new Exception("Pack Up must discard up to 2.");
packBalance.UpgradeInternal();
if (packBalance.DynamicVars.Cards.IntValue != 3) throw new Exception("Pack Up upgrade must discard up to 3.");
var teamBalance = ModelDb.Card<DuchessDuchess>().ToMutable();
if (teamBalance.EnergyCost.CostsX || teamBalance.EnergyCost.Canonical != 1 || teamBalance.Rarity != CardRarity.Rare
    || teamBalance.DynamicVars["AllyDodgeDrawX"].IntValue != 2)
    throw new Exception("Duchess multiplayer card must be a 1-cost Rare with two tokens/draws.");
teamBalance.UpgradeInternal();
if (teamBalance.EnergyCost.GetResolved() != 0 || teamBalance.DynamicVars["AllyDodgeDrawX"].IntValue != 2)
    throw new Exception("Duchess multiplayer upgrade must reduce cost to 0 without changing count.");
Console.WriteLine("PASS: revised X cards, calculated volley damage, Featherstep 2/1, Pack Up 2/3, and multiplayer Duchess 1/0 Rare.");
var wraithJar = ModelDb.Potion<WraithJar>().ToMutable();
if (wraithJar.TargetType != TargetType.AnyEnemy || wraithJar.DynamicVars.Damage.BaseValue != 30)
    throw new Exception("Wraith Jar must target an enemy and deal 30 damage.");
SpiritCallingJar CapturedJar(string entry)
{
    var jar = (SpiritCallingJar)ModelDb.Potion<SpiritCallingJar>().ToMutable();
    jar.SetCapturedMonster(new ModelId(ModelId.SlugifyCategory<MonsterModel>(), entry));
    return jar;
}
PotionModel[] capturedSlots = { CapturedJar("BYRDONIS"), null!, CapturedJar("FROG_KNIGHT") };
SerializableRelic captureEnvelope = RevenantSpiritJarPersistence.CreateEnvelope(capturedSlots);
var nativeJsonContext = (JsonSerializerContext)typeof(PotionModel).Assembly
    .GetType("MegaCrit.Sts2.Core.Saves.MegaCritSerializerContext", true)!
    .GetProperty("Default", flags)!.GetValue(null)!;
var relicJsonInfo = nativeJsonContext.GetTypeInfo(typeof(SerializableRelic))!;
string captureSaveJson = JsonSerializer.Serialize(captureEnvelope, relicJsonInfo);
if (!captureSaveJson.Contains("BYRDONIS") || !captureSaveJson.Contains("FROG_KNIGHT")
    || !captureSaveJson.Contains("MONSTER"))
    throw new Exception("Native save JSON must retain both exact full monster IDs.");
var jsonEnvelope = (SerializableRelic)JsonSerializer.Deserialize(captureSaveJson, relicJsonInfo)!;
// The standalone fixture skips workshop discovery; register the envelope entry
// in the same native cache that the game's mod-aware Init fills at startup.
var netEntryMap = (Dictionary<string, int>)typeof(ModelIdSerializationCache)
    .GetField("_entryNameToNetIdMap", flags)!.GetValue(null)!;
var netEntries = (List<string>)typeof(ModelIdSerializationCache)
    .GetField("_netIdToEntryNameMap", flags)!.GetValue(null)!;
if (!netEntryMap.ContainsKey(captureEnvelope.Id!.Entry))
{
    netEntryMap[captureEnvelope.Id.Entry] = netEntries.Count;
    netEntries.Add(captureEnvelope.Id.Entry);
}
typeof(ModelIdSerializationCache).GetProperty("EntryIdBitSize", flags)!.SetValue(null, 16);
var packetWriter = new PacketWriter();
NightMustStay.Core.Compatibility.Sts2BranchCompat.RegisterSavedPropertyType(typeof(RevenantSpiritJarSaveData));
captureEnvelope.Serialize(packetWriter);
var packetReader = new PacketReader();
packetReader.Reset(packetWriter.Buffer);
var packetEnvelope = new SerializableRelic();
packetEnvelope.Deserialize(packetReader);
foreach (SerializableRelic envelope in new[] { jsonEnvelope, packetEnvelope })
{
    PotionModel[] restoredSlots =
    {
        ModelDb.Potion<SpiritCallingJar>().ToMutable(), null!, ModelDb.Potion<SpiritCallingJar>().ToMutable(),
    };
    RevenantSpiritJarPersistence.Restore(restoredSlots, new[] { envelope });
    if (((SpiritCallingJar)restoredSlots[0]).CapturedMonsterEntry != "BYRDONIS"
        || ((SpiritCallingJar)restoredSlots[2]).CapturedMonsterEntry != "FROG_KNIGHT")
        throw new Exception("Cross-act save or multiplayer restore lost or swapped captured monster identities.");
    // A second act/save round trip must preserve the already-restored identities.
    SerializableRelic secondAct = RevenantSpiritJarPersistence.CreateEnvelope(restoredSlots);
    if (!JsonSerializer.Serialize(secondAct, relicJsonInfo).Contains("FROG_KNIGHT"))
        throw new Exception("Captured monster identity must survive repeated act transitions.");
}
var emptyCallingJar = (SpiritCallingJar)ModelDb.Potion<SpiritCallingJar>().ToMutable();
if (emptyCallingJar.PassesCustomUsabilityCheck || emptyCallingJar.Rarity != MegaCrit.Sts2.Core.Entities.Potions.PotionRarity.Token
    || emptyCallingJar.CanBeGeneratedInCombat || emptyCallingJar.TargetType != TargetType.Self)
    throw new Exception("An empty generated-only Spirit Calling Jar must not be usable or randomly generated.");
emptyCallingJar.SetCapturedMonster(new ModelId("MONSTER", "MISSING_REMOVED_MOD_MONSTER"));
if (emptyCallingJar.TryGetCapturedMonster(out _) || emptyCallingJar.PassesCustomUsabilityCheck)
    throw new Exception("Missing monsters must fail safely without random summons.");
emptyCallingJar.SetCapturedMonster(ModelDb.Potion<WraithJar>().Id);
if (emptyCallingJar.TryGetCapturedMonster(out _))
    throw new Exception("Wrong-category captured IDs must not throw or resolve as monsters.");
var capturedMonsterType = typeof(PotionModel).Assembly.GetType("MegaCrit.Sts2.Core.Models.Monsters.Byrdonis", true)!;
if (!ModelDb.Contains(capturedMonsterType)) typeof(ModelDb).GetMethod("Inject", flags)!.Invoke(null, new object[] { capturedMonsterType });
emptyCallingJar.SetCapturedMonster(ModelDb.GetId(capturedMonsterType));
if (!emptyCallingJar.TryGetCapturedMonster(out MonsterModel exactMonster)
    || exactMonster.Id != ModelDb.GetId(capturedMonsterType) || !emptyCallingJar.PassesCustomUsabilityCheck)
    throw new Exception("Spirit Calling Jar must resolve the exact canonical monster from the saved ID.");
var captureSnapshot = new SerializablePlayer { Relics = new() { jsonEnvelope } };
RevenantSpiritJarLoadPatch.ExcludeSaveOnlyEnvelope(captureSnapshot, out var captureLoadState);
if (captureSnapshot.Relics.Count != 0 || captureLoadState.Envelopes.Length != 1)
    throw new Exception("Save-only capture envelopes must not enter the real relic inventory.");
RevenantSpiritJarLoadPatch.PreserveInputSnapshot(captureSnapshot, captureLoadState, null!);
if (captureSnapshot.Relics.Count != 1)
    throw new Exception("Capture loading must not mutate the input save snapshot.");
var killingBlow = typeof(WraithJar).GetMethod("IsKillingBlow", flags)!;
var killTargetFixture = (Creature)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Creature));
var targetHpField = typeof(Creature).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic)!;
foreach (var (hp, killed, expected) in new[] { (0, true, true), (5, false, false), (0, false, false), (5, true, false) })
{
    targetHpField.SetValue(killTargetFixture, hp);
    var result = new DamageResult(killTargetFixture, ValueProp.Unpowered) { WasTargetKilled = killed };
    if ((bool)killingBlow.Invoke(null, new object[] { killTargetFixture, new[] { result } })! != expected)
        throw new Exception("Wraith Jar must award a captured jar only for its own lethal damage, not survivors or resurrected targets.");
}
Console.WriteLine("PASS: Wraith Jar damage/target, distinct captured IDs, native JSON and network round trips, repeated act transitions, and stale-ID safety.");
var necroHp = typeof(RevenantSummonManager).GetMethod("CalculateNecroMaxHp", flags)!;
foreach (bool elite in new[] { false, true })
{
    if ((int)necroHp.Invoke(null, new object[] { elite })! != (elite ? 40 : 20))
        throw new Exception("Necro HP must be fixed at 20/40, independent of source monster HP.");
    foreach (var action in new[] { RevenantFamilyAction.First, RevenantFamilyAction.Second })
    {
        var necro = new RevenantNecro
        {
            SourceMonster = null!, Creature = null!, IsElite = elite, ScheduledAction = action,
        };
        int damage = action == RevenantFamilyAction.First ? 8 : elite ? 6 : 3;
        int hits = action == RevenantFamilyAction.First && elite ? 2 : 1;
        if (necro.DamagePerHit != damage || necro.HitCount != hits)
            throw new Exception($"Incorrect Necro action: elite={elite}, action={action}.");
    }
}
foreach (var power in new PowerModel[]
    { ModelDb.Power<NecroAttackPower>().ToMutable(), ModelDb.Power<NecroProtectPower>().ToMutable() })
{
    power.DynamicVars["Damage"].BaseValue = 6;
    power.DynamicVars["Hits"].BaseValue = 2;
    if (power.DynamicVars["Damage"].PreviewValue != 6 || power.DynamicVars["Hits"].PreviewValue != 2)
        throw new Exception("Necro action hover tips must reflect the scheduled action's values.");
}
if (typeof(NecromancyPower).GetMethod("AfterSideTurnEnd")!.DeclaringType != typeof(NecromancyPower)
    || typeof(NecromancyPower).GetMethod("AfterSideTurnStart")!.DeclaringType == typeof(NecromancyPower))
    throw new Exception("Necromancy must decay at turn end, never turn start.");
Console.WriteLine("PASS: ordinary/elite Necro HP, both actions, dynamic action hover tips, and end-turn decay hook.");
var revenantTeamCard = ModelDb.Card<RevenantCard>().ToMutable();
if (revenantTeamCard.EnergyCost.Canonical != 1)
    throw new Exception("Revenant's multiplayer card must cost 1.");
var reanimateCard = ModelDb.Card<ReanimateDead>().ToMutable();
if (reanimateCard.TargetType != TargetType.Self || reanimateCard.EnergyCost.Canonical != 1
    || !reanimateCard.Keywords.Contains(CardKeyword.Exhaust))
    throw new Exception("Reanimate Dead must summon without selecting a target and exhaust.");
reanimateCard.UpgradeInternal();
if (reanimateCard.EnergyCost.GetResolved() != 0 || !reanimateCard.Keywords.Contains(CardKeyword.Exhaust))
    throw new Exception("Reanimate Dead must preserve its 0-cost upgrade and Exhaust.");
var manipulation = ModelDb.Card<SpiritManipulation>().ToMutable();
if (manipulation.DynamicVars.Damage.BaseValue != 14)
    throw new Exception("Spirit Manipulation must deal 14 damage.");
manipulation.UpgradeInternal();
if (manipulation.DynamicVars.Damage.BaseValue != 19)
    throw new Exception("Spirit Manipulation must upgrade to 19 damage.");
string revenantTextSource = File.ReadAllText("src/Core/Models/Cards/RevenantTextTableCards.cs");
string reanimateSource = revenantTextSource.Split("public sealed class ReanimateDead")[1].Split("public sealed class SoulReturn")[0];
if (!reanimateSource.Contains("SummonRandomNecro(context)") || reanimateSource.Contains("ChooseFamilyAndCall")
    || reanimateSource.Contains("ReviveDeadEnemy"))
    throw new Exception("Reanimate Dead must summon a random Necro, not a Family member or a required corpse.");
foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
{
    using JsonDocument relicText = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/relics.json"));
    foreach (JsonProperty title in relicText.RootElement.EnumerateObject().Where(entry => entry.Name.EndsWith(".title")
        && !entry.Name.StartsWith("SEA_GLASS.")))
    {
        string flavorKey = title.Name[..^6] + ".flavor";
        if (!relicText.RootElement.TryGetProperty(flavorKey, out JsonElement flavor) || flavor.ValueKind != JsonValueKind.String)
            throw new Exception($"Relic inspection requires {locale}:{flavorKey}, even when the flavor is empty.");
    }
    string nativeFlavorKey = ModelDb.Relic<DuchessPrimalGlintstoneBlade>().Flavor.LocEntryKey;
    if (!relicText.RootElement.TryGetProperty(nativeFlavorKey, out _))
        throw new Exception("Primal Glintstone Blade's native inspection flavor lookup must resolve.");
}
if (!revenantTextSource.Contains("HoverTipFactory.Static(StaticHoverTip.Fatal)")
    || !File.ReadAllText("src/Core/Models/Cards/DuchessCard.cs").Contains("HoverTipFactory.Static(StaticHoverTip.Fatal)"))
    throw new Exception("Both Fatal cards must reuse the native Fatal hover tip.");
Console.WriteLine("PASS: relic inspection flavor keys in four locales, Revenant card costs, random summoning, and Spirit Manipulation 14/19 with native Fatal tips.");
CardGlossaryRegression.Run();
foreach (var character in new CharacterModel[] {
    ModelDb.Character<Guardian>(), ModelDb.Character<Ironeye>(),
    ModelDb.Character<Revenant>(), duchess })
{
    if (character.MapDrawingColor != character.CardPool.DeckEntryCardColor)
        throw new Exception($"{character.Id} drawing and deck colors must match.");
}
Console.WriteLine("PASS: four Nightfarer drawing colors match their deck colors.");
// UI source guards: refreshing a portrait must not overwrite radio selection
// visuals; history fitting must not introduce nonuniform scaling.
string libraryPortraitSource = File.ReadAllText("src/Core/Patches/GuardianProgressPatch.cs");
if (System.Text.RegularExpressions.Regex.Matches(libraryPortraitSource,
        "filter.IsSelected \\? 1f : 0.3f").Count != 3
    || System.Text.RegularExpressions.Regex.Matches(libraryPortraitSource,
        "filter.IsSelected \\? 1f : 0.55f").Count != 3)
    throw new Exception("All three original Nightfarer filters must preserve native selection shading.");
string historyIconSource = File.ReadAllText("src/Core/Patches/DuchessHistoryIconPatch.cs");
if (!historyIconSource.Contains("TextureRect.StretchModeEnum.KeepAspectCentered")
    || !historyIconSource.Contains("TextureRect.ExpandModeEnum.IgnoreSize")
    || !historyIconSource.Contains("OriginalLayouts.Remove(icon)")
    || historyIconSource.Contains("icon.Scale ="))
    throw new Exception("Duchess history icon must preserve aspect ratio and restore reused controls.");
Console.WriteLine("PASS: history icon and card-library selection source guards.");
// Revenant lightning balance and transition boundary regression.
var discardLightning = ModelDb.Card<LightningStrike>().ToMutable();
if (discardLightning.EnergyCost.Canonical != ModelDb.Card<DuchessFallingMagic>().EnergyCost.Canonical
    || discardLightning.DynamicVars.Damage.BaseValue != 6
    || (bool)typeof(LightningStrike).GetProperty("IsPlayable", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(discardLightning)!)
    throw new Exception("Lightning Strike must be unplayable with 6 discard damage.");
var discardTrigger = typeof(LightningStrike).GetMethod("ShouldTriggerOnDiscard", BindingFlags.Static | BindingFlags.NonPublic)!;
foreach (PileType oldPile in Enum.GetValues<PileType>())
foreach (PileType newPile in Enum.GetValues<PileType>())
    if ((bool)discardTrigger.Invoke(null, new object[] { oldPile, newPile })!
        != (newPile == PileType.Discard && oldPile != PileType.Discard))
        throw new Exception($"Incorrect Lightning Strike transition: {oldPile} -> {newPile}");
discardLightning.UpgradeInternal();
if (discardLightning.DynamicVars.Damage.BaseValue != 9) throw new Exception("Lightning Strike upgrade must deal 9.");
var ancientLightning = (AncientDragonLightning)ModelDb.Card<AncientDragonLightning>().ToMutable();
if (ancientLightning.EnergyCost.Canonical != 2 || ancientLightning.EnergyCost.CostsX
    || ancientLightning.DynamicVars.Damage.BaseValue != 6 || ancientLightning.DynamicVars.Repeat.IntValue != 4
    || ancientLightning.DynamicVars["ChargeHits"].IntValue != 5)
    throw new Exception("Ancient Dragon Lightning must cost 2 and hit 4/9 times for 6.");
ancientLightning.ChargeComplete = true;
if (ancientLightning.TargetType != TargetType.RandomEnemy || !((BoolVar)ancientLightning.DynamicVars["Ready"]).BoolVal)
    throw new Exception("Ancient Dragon Lightning must expose its charged state.");
ancientLightning.UpgradeInternal();
if (ancientLightning.DynamicVars.Damage.BaseValue != 7) throw new Exception("Ancient Dragon Lightning upgrade must deal 7.");
var deathLightning = (DeathLightning)ModelDb.Card<DeathLightning>().ToMutable();
if (deathLightning.EnergyCost.Canonical != 1 || deathLightning.DynamicVars.Damage.BaseValue != 5
    || deathLightning.DynamicVars.Repeat.IntValue != 2 || deathLightning.DynamicVars["ChargeHits"].IntValue != 2
    || deathLightning.DynamicVars.Cards.IntValue != 1) throw new Exception("Death Lightning base values changed incorrectly.");
deathLightning.UpgradeInternal();
if (deathLightning.DynamicVars.Damage.BaseValue != 5 || deathLightning.DynamicVars.Repeat.IntValue != 2
    || deathLightning.DynamicVars["ChargeHits"].IntValue != 3 || deathLightning.DynamicVars.Cards.IntValue != 2)
    throw new Exception("Death Lightning upgrade must only increase charged hits and recovery.");
var iceSpear = ModelDb.Card<IceLightningSpear>().ToMutable();
if (iceSpear.DynamicVars.Damage.BaseValue != 7 || iceSpear.DynamicVars["Freeze"].IntValue != 2)
    throw new Exception("Ice Lightning Spear base must deal 7 and apply 2 Frostbite.");
iceSpear.UpgradeInternal();
if (iceSpear.DynamicVars.Damage.BaseValue != 9 || iceSpear.DynamicVars["Freeze"].IntValue != 3)
    throw new Exception("Ice Lightning Spear upgrade must deal 9 and apply 3 Frostbite.");
var ghostTouch = ModelDb.Card<GhostlyTouch>().ToMutable();
if (ghostTouch.DynamicVars["Freeze"].IntValue != 2 || ghostTouch.Keywords.Contains(CardKeyword.Innate))
    throw new Exception("Ghostly Touch base must apply 2 Frostbite without Innate.");
ghostTouch.UpgradeInternal();
if (ghostTouch.DynamicVars["Freeze"].IntValue != 2 || !ghostTouch.Keywords.Contains(CardKeyword.Innate))
    throw new Exception("Ghostly Touch upgrade must add Innate without increasing Frostbite.");
var fortissax = ModelDb.Card<FlannSaxLightningSpear>().ToMutable();
if (fortissax.EnergyCost.Canonical != 3 || fortissax.TargetType != TargetType.AllEnemies
    || fortissax.DynamicVars.Damage.BaseValue != 10 || fortissax.DynamicVars.Repeat.IntValue != 2
    || !fortissax.Keywords.Contains(CardKeyword.Exhaust)) throw new Exception("Fortissax base values or target are incorrect.");
fortissax.UpgradeInternal();
if (fortissax.DynamicVars.Damage.BaseValue != 14 || fortissax.DynamicVars.Repeat.IntValue != 2)
    throw new Exception("Fortissax upgrade must only increase damage to 14.");
var lansseax = ModelDb.Card<LansseaxBlade>().ToMutable();
if (lansseax.TargetType != TargetType.AllEnemies || lansseax.DynamicVars.Damage.BaseValue != 42)
    throw new Exception("Lansseax must deal 42 AOE damage.");
lansseax.UpgradeInternal();
if (lansseax.EnergyCost.GetResolved() != 4 || lansseax.DynamicVars.Damage.BaseValue != 42)
    throw new Exception("Lansseax upgrade must retain cost reduction only.");
var beaststone = ModelDb.Card<Beaststone>().ToMutable();
if (beaststone.DynamicVars.Damage.BaseValue != 7) throw new Exception("Beaststone must deal 7.");
beaststone.UpgradeInternal();
if (beaststone.DynamicVars.Damage.BaseValue != 9 || beaststone.DynamicVars["Strength"].IntValue != 2)
    throw new Exception("Beaststone must preserve its existing upgrade.");
if (ModelDb.Card<SoulChargingClaw>().DynamicVars["Weak"].IntValue != 3)
    throw new Exception("Soul Charging Claw must apply 3 Weak.");
Console.WriteLine("PASS: nine Revenant balance changes and all Lightning Strike pile-transition pairs.");
foreach (var kind in Enum.GetValues<NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind>())
{
    var cues = NightMustStay.Core.Nodes.Vfx.DuchessAudio.AttackCues(kind);
    if (cues.Length != 2 || cues[0].At > cues[1].At
        || cues.Any(cue => cue.At < 0 || cue.At >= 1 || cue.Volume <= 0 || cue.Volume > 1
            || !cue.File.EndsWith(".mp3", StringComparison.Ordinal)))
        throw new Exception($"Invalid Duchess audio timeline: {kind}");
}
var clockAudio = NightMustStay.Core.Nodes.Vfx.DuchessAudio.AttackCues(
    NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Clock);
if (clockAudio[1].At != .48f || clockAudio[1].File != "glass_orb_evoke.mp3")
    throw new Exception("Clock shatter sound must match the .48 visual break cue.");
if (!NightMustStay.Core.Nodes.Vfx.DuchessAudio.AttackCues(NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Greatbow)
    .SequenceEqual(NightMustStay.Core.Nodes.Vfx.DuchessAudio.AttackCues(NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Mastery)))
    throw new Exception("Loretta's spells must share the same bow sound palette.");
Console.WriteLine("PASS: all Duchess VFX audio timelines and clock/bow synchronization.");
foreach (var (card, expected) in new (CardModel, NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind)[] {
    (ModelDb.Card<DuchessRadiantBlade>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Glintblade),
    (ModelDb.Card<DuchessCarianSlicer>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Slicer),
    (ModelDb.Card<DuchessGreatCaria>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.GreatCaria),
    (ModelDb.Card<DuchessCarianGreatsword>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Greatsword),
    (ModelDb.Card<DuchessCarianPiercer>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Piercer),
    (ModelDb.Card<DuchessRestage>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Clock),
    (ModelDb.Card<DuchessReenactment>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Clock),
    (ModelDb.Card<DuchessFleetingInstant>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Clock),
    (ModelDb.Card<DuchessLorettaGreatbow>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Greatbow),
    (ModelDb.Card<DuchessLorettaMastery>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Mastery),
    (ModelDb.Card<DuchessDeathBlade>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.DeathBlade),
    (ModelDb.Card<DuchessGoldenBlade>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.GoldenBlade),
    (ModelDb.Card<DuchessMiquellasHalo>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Miquella),
    (ModelDb.Card<DuchessSacredHalo>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.Sacred),
    (ModelDb.Card<DuchessLorettaSlash>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.LorettaSlash),
    (ModelDb.Card<DuchessSilverStorm>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.SilverStorm),
    (ModelDb.Card<DuchessOpeningMoment>(), NightMustStay.Core.Nodes.Vfx.DuchessAttackVfx.Kind.OpeningMoment) })
{
    if (NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.KindFor(card) != expected)
        throw new Exception($"Incorrect Duchess VFX route: {card.Id}");
}
Console.WriteLine("PASS: all Duchess spell/reprise and three additional attack cards have explicit visual routes.");
if (duchess.Id.Entry != "DUCHESS"
    || duchess.StartingHp != 66
    || duchess.StartingGold != 99
    || duchess.StartingRelics is not [DuchessOldPocketwatch])
{
    throw new Exception("Duchess character-select identity or starting loadout is incomplete.");
}
if (ModelDb.Relic<DuchessReversePocketwatch>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Ancient
    || ModelDb.Relic<DuchessCrownBadge>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Common
    || ModelDb.Relic<DuchessGoldenDewdrop>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Uncommon
    || ModelDb.Relic<DuchessPrimalGlintstoneBlade>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Uncommon
    || ModelDb.Relic<DuchessBlessedIronCoin>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Rare
    || ModelDb.Relic<DuchessNightOfWisdom>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Rare
    || ModelDb.Relic<DuchessBlueStainedBlade>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Shop
    || ModelDb.Relic<DuchessCarianBadge>().Rarity != MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Rare
    || ModelDb.Potion<DuchessSmokeBottle>().Rarity != MegaCrit.Sts2.Core.Entities.Potions.PotionRarity.Common
    || ModelDb.Potion<DuchessRadiantBladeCrystal>().Rarity != MegaCrit.Sts2.Core.Entities.Potions.PotionRarity.Uncommon
    || ModelDb.Potion<DuchessRegretPotion>().Rarity != MegaCrit.Sts2.Core.Entities.Potions.PotionRarity.Rare)
    throw new Exception("The new Duchess relic and potion rarities must match the approved list.");
var refinement = new Dictionary<ModelId, RelicModel>();
typeof(GuardianTouchOfOrobasPatch).GetMethod("AddNightreignRefinements", flags)!
    .Invoke(null, new object[] { refinement });
if (!refinement.TryGetValue(ModelDb.Relic<DuchessOldPocketwatch>().Id, out var improvedWatch)
    || improvedWatch.Id != ModelDb.Relic<DuchessReversePocketwatch>().Id)
    throw new Exception("Orobas must upgrade the old pocketwatch into the reverse pocketwatch.");
foreach (string obsolete in new[] { "DuchessMendedPocketwatch", "DuchessLaceCuff", "DuchessSilverThimble",
    "DuchessDanceShoes", "DuchessUnsentLetter", "DuchessBlueRibbon", "DuchessSilverPerfume",
    "DuchessVeilVial", "DuchessMemoryDraught" })
    if (typeof(Duchess).Assembly.GetType("NightMustStay.Core.Models.Relics." + obsolete) != null
        || typeof(Duchess).Assembly.GetType("NightMustStay.Core.Models.Potions." + obsolete) != null)
        throw new Exception("Obsolete Duchess item model remains: " + obsolete);

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
            "AllyBlock" or "BlockPerExhaust" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 || entry.Value.ConcealedTripleDamage
                || entry.Value.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage") => "CalculationBase",
            "ConcealedBonusDamage" or "MomentBonusDamage" => "ExtraDamage",
            "MomentDamage" => effect.Amount > 0 ? "ExtraDamage" : "CalculationBase",
            "RewindDamage" => "ExtraDamage",
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
            "AllyBlock" or "BlockPerExhaust" => "Block",
            "Damage" when entry.Value.RestageDivisor > 0 || entry.Value.ConcealedTripleDamage
                || entry.Value.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage") => "CalculationBase",
            "ConcealedBonusDamage" or "MomentBonusDamage" => "ExtraDamage",
            "MomentDamage" => effect.Amount > 0 ? "ExtraDamage" : "CalculationBase",
            "RewindDamage" => "ExtraDamage",
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
    "LightningNerves CarianSwordsmanship GrandReprise Feint SwayingStep Invitation InsightFuture PassingCut Initiative " +
    "PoisedExit GapMoonshadow MagicRadiantBlade RadiantBladeArray AngelWings CariaPhalanx GreatswordPhalanx " +
    "MiquellasHalo GoldenBlade CarianGreatsword CarianPiercer RadiantBladeMagic DeathBlade GlintstoneHail " +
    "CarianRetaliation FallingMagic Pivot Restage Reverberation ThreadTheGap QuickHands Escape " +
    "CalmComposure TidyCollar SoftLanding Distraction VeiledStep SilverFlash Beat Composure Silence " +
    "MidnightWaltz SilverStorm ThiefsArsenal Finale Duchess GlintstoneKnife HiddenPocket EternalRestage " +
    "GrandBearing GoldenMoment LorettaMastery LorettaGreatbow SleightOfHand BecomeInvisible BlindSpot " +
    "FleetingInstant MomentAndEternity EternalForm ShadowSword Quietude ParallelTime " +
    "Memory InchVictory LorettaSlash SacredHalo GracefulSwordDance MemoryFragment PhantomKiller GreatCaria Fate")
    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
var expectedIds = tableIds.Select(id => "Duchess" + id)
    .Concat(new[] { nameof(DuchessStrike), nameof(DuchessDefend),
        nameof(DuchessRadiantBlade), nameof(DuchessDodge) }).ToHashSet();
if (tableIds.Length != 86 || !expectedIds.SetEquals(DuchessCardCatalog.All.Keys))
    throw new Exception("Duchess card IDs differ from the user-approved table and additions.");
var instant = ModelDb.Card<DuchessFleetingInstant>().ToMutable();
var whirlingStrike = ModelDb.Card<WhirlingStrike>().ToMutable();
var countWhirlingBonus = typeof(WhirlingStrike).GetMethod("CountAdditionalHits", flags)!;
// This headless test has no localization singleton. Only supply titles for the
// duration of the name-filter fixture; production filtering remains unchanged.
var whirlingTitleFixture = new HarmonyLib.Harmony("NightMustStay.WhirlingStrike.TitleFixture");
whirlingTitleFixture.Patch(HarmonyLib.AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Title)),
    prefix: new HarmonyLib.HarmonyMethod(typeof(WhirlingTitleFixture).GetMethod(nameof(WhirlingTitleFixture.Prefix))!));
try
{
foreach (int defendCount in new[] { 0, 1, 3 })
{
    CardModel[] hand = Enumerable.Range(0, defendCount)
        .Select(_ => ModelDb.Card<DefendGuardian>().ToMutable())
        .Append(ModelDb.Card<StrikeGuardian>().ToMutable()).ToArray();
    int extraHits = (int)countWhirlingBonus.Invoke(null, new object[] { hand })!;
    decimal previewHits = whirlingStrike.DynamicVars["CalculationBase"].BaseValue
        + whirlingStrike.DynamicVars["CalculationExtra"].BaseValue * extraHits;
    if (previewHits != defendCount + 1)
        throw new Exception("Whirling Strike must have 1 base hit plus 1 per Defend card.");
}
}
finally { whirlingTitleFixture.UnpatchAll(whirlingTitleFixture.Id); }
if (whirlingStrike.TargetType != TargetType.AllEnemies || whirlingStrike.DynamicVars.Damage.BaseValue != 5
    || ((CalculatedVar)whirlingStrike.DynamicVars["CalculatedHits"]).Calculate(null) != 1)
    throw new Exception("Whirling Strike must be a 5-damage AOE with one base hit.");
whirlingStrike.UpgradeInternal();
if (whirlingStrike.DynamicVars.Damage.BaseValue != 7)
    throw new Exception("Whirling Strike must preserve its 7-damage upgrade.");
var curtainCall = ModelDb.Card<CurtainCall>().ToMutable();
if (curtainCall.TargetType != TargetType.AllEnemies || curtainCall.EnergyCost.Canonical != 1
    || curtainCall.DynamicVars.Damage.BaseValue != 5)
    throw new Exception("Curtain Call must remain a 1-cost 5-damage AOE.");
curtainCall.UpgradeInternal();
if (curtainCall.DynamicVars.Damage.BaseValue != 8)
    throw new Exception("Curtain Call must preserve its 8-damage upgrade.");
string curtainSource = File.ReadAllText("src/Core/Models/Cards/IroneyeCards67To70.cs");
curtainSource = curtainSource[..curtainSource.IndexOf("public sealed class AirRendingArrow", StringComparison.Ordinal)];
if (!curtainSource.Contains(".TargetingAllOpponents(CombatState)")
    || !curtainSource.Contains(".WithHitCount(retainedCards)")
    || curtainSource.Contains("Rng.CombatTargets") || !curtainSource.Contains("PlayerCmd.EndTurn"))
    throw new Exception("Curtain Call must use native multi-hit AOE, never a random target, and still end the turn.");
var shieldImpactPreview = ModelDb.Card<ShieldImpact>().ToMutable();
if (shieldImpactPreview.DynamicVars["CalculatedDamage"] is not CalculatedDamageVar
    || shieldImpactPreview.DynamicVars["CalculationBase"].BaseValue != 0
    || shieldImpactPreview.DynamicVars["ExtraDamage"].BaseValue != 1
    || shieldImpactPreview.DynamicVars.CalculatedDamage.Calculate(null) != 0)
    throw new Exception("Shield Impact must reuse BodySlam's native calculated damage variables and preview hooks.");
// Exercise the exact native multiplier callback with zero, one, and many rewind steps.
var rewindPlayer = (Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Player));
var rewindCreature = new Creature(rewindPlayer, 70, 70);
typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
    .SetValue(rewindPlayer, rewindCreature);
var necroPetFixture = (Creature)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Creature));
typeof(Creature).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(necroPetFixture, 20);
typeof(Creature).GetField("<Side>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
    .SetValue(necroPetFixture, MegaCrit.Sts2.Core.Combat.CombatSide.Player);
necroPetFixture.PetOwner = rewindPlayer;
var shouldDecay = typeof(NecromancyPower).GetMethod("ShouldDecay", flags)!;
foreach (var (side, participants, expected) in new[]
{
    (MegaCrit.Sts2.Core.Combat.CombatSide.Player, new[] { rewindCreature }, true),
    (MegaCrit.Sts2.Core.Combat.CombatSide.Player, Array.Empty<Creature>(), false),
    (MegaCrit.Sts2.Core.Combat.CombatSide.Enemy, new[] { rewindCreature }, false),
})
    if ((bool)shouldDecay.Invoke(null, new object[] { necroPetFixture, side, participants })! != expected)
        throw new Exception("Necromancy must decay when the pet owner's turn ends, not another player's or the enemy turn.");
Console.WriteLine("PASS: Necromancy recognizes player-only native turn participants and independent multiplayer turns.");

var summonManager = RevenantSummonManager.For(rewindPlayer);
var summonNecroFixture = new Creature(rewindPlayer, 20, 20) { PetOwner = rewindPlayer };
summonManager.RegisterNecro(new RevenantNecro { SourceMonster = null!, Creature = summonNecroFixture });
var summonFamilyFixture = new Creature(rewindPlayer, 11, 11) { PetOwner = rewindPlayer };
var familyField = typeof(RevenantSummonManager).GetField("_familyCreature", BindingFlags.Instance | BindingFlags.NonPublic)!;
var sacrifice = ModelDb.Card<FrenziedFlame>().ToMutable();
sacrifice.Owner = rewindPlayer;
var sacrificeMultiplier = (Func<CardModel, Creature?, decimal>)typeof(CalculatedVar)
    .GetField("_multiplierCalc", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sacrifice.DynamicVars.CalculatedDamage)!;
var playableGetter = typeof(CardModel).GetProperty("IsPlayable", BindingFlags.Instance | BindingFlags.NonPublic)!.GetMethod!;
foreach (CardModel summonCard in new CardModel[]
{
    sacrifice, ModelDb.Card<BurnLife>().ToMutable(), ModelDb.Card<UndyingMarch>().ToMutable(),
    ModelDb.Card<UnbearableFrenzy>().ToMutable(), ModelDb.Card<SpaceRendingFrenzy>().ToMutable(),
})
{
    if (summonCard.Owner == null) summonCard.Owner = rewindPlayer;
    if (!(bool)playableGetter.Invoke(summonCard, null)!)
        throw new Exception($"{summonCard.GetType().Name} must be playable with only a living Necro.");
}
if (summonManager.GetLivingSummons().Count != 1 || sacrificeMultiplier(sacrifice, null) != 20)
    throw new Exception("Sacrifice must include a Necro without a Family member.");
familyField.SetValue(summonManager, summonFamilyFixture);
if (summonManager.GetLivingSummons().Count != 2 || sacrificeMultiplier(sacrifice, null) * 2 != 62)
    throw new Exception("Frenzied Flame must sum both summons' HP, not just Family HP.");
var summonUndying = ModelDb.Power<UndyingMarchPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(summonUndying, summonNecroFixture);
((List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!
    .GetValue(summonNecroFixture)!).Add(summonUndying);
if (sacrificeMultiplier(sacrifice, null) * 2 != 60 || summonUndying.ShouldDie(summonNecroFixture))
    throw new Exception("Undying March must protect Necros and exclude their remaining 1 HP from sacrifice previews.");
targetHpField.SetValue(summonNecroFixture, 0);
if (summonManager.GetLivingSummons().Count != 1 || !summonManager.IsNecroCreature(summonNecroFixture))
    throw new Exception("Dead summons must be excluded from buffs but remain identifiable for HP-loss triggers.");
targetHpField.SetValue(summonFamilyFixture, 0);
if ((bool)playableGetter.Invoke(sacrifice, null)! || sacrificeMultiplier(sacrifice, null) != 0)
    throw new Exception("Summon sacrifice must be unplayable without a living summon.");
familyField.SetValue(summonManager, null);
if (ModelDb.Card<UnbearableFrenzy>().ToMutable().DynamicVars["FamilyDamage"].BaseValue != 6
    || ModelDb.Card<SpaceRendingFrenzy>().ToMutable().DynamicVars["FamilyDamage"].BaseValue != 4)
    throw new Exception("Summon HP costs must be 6 for Unbearable Frenzy and 4 for Space-Rending Frenzy.");
Console.WriteLine("PASS: Necro-only playability, both-summon sacrifice, Undying March, dead-summon filtering and revised HP costs.");
typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
    .SetValue(rewindPlayer, rewindCreature);
var rewindMoment = (DuchessMomentPower)ModelDb.Power<DuchessMomentPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(rewindMoment, rewindCreature);
((List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!
    .GetValue(rewindCreature)!).Add(rewindMoment);
instant.Owner = rewindPlayer;
var rewindMultiplier = (Func<CardModel, Creature?, decimal>)typeof(CalculatedVar)
    .GetField("_multiplierCalc", BindingFlags.Instance | BindingFlags.NonPublic)!
    .GetValue(instant.DynamicVars.CalculatedDamage)!;
foreach (int stepCount in new[] { 0, 1, 12 })
{
    rewindMoment.SetAmount(stepCount + 1, false);
    decimal steps = rewindMultiplier(instant, null);
    decimal damage = instant.DynamicVars["CalculationBase"].BaseValue
        + instant.DynamicVars["ExtraDamage"].BaseValue * steps;
    if (steps != stepCount || damage != 6 + 3 * stepCount)
        throw new Exception("Fleeting Instant must preview 6 + 3 damage per rewind step.");
}
var transcendence = new Dictionary<ModelId, CardModel>();
typeof(GuardianArchaicToothPatch).GetMethod("AddGuardianTranscendence", flags)!
    .Invoke(null, new object[] { transcendence });
if (!transcendence.TryGetValue(ModelDb.Card<DuchessElegantBearing>().Id, out var transformed)
    || transformed.Id != ModelDb.Card<DuchessGrandBearing>().Id
    || transcendence.ContainsKey(ModelDb.Card<DuchessBladeRevealMoment>().Id))
    throw new Exception("Orobas must transform Elegant Bearing into Grand Bearing, not Eternal Restage.");
var ancientDuchessCards = DuchessCardCatalog.All
    .Where(entry => entry.Value.Rarity == CardRarity.Ancient).Select(entry => entry.Key).ToHashSet();
if (!ancientDuchessCards.SetEquals(new[] { nameof(DuchessGrandBearing), nameof(DuchessEternalRestage) })
    || transcendence.ContainsKey(ModelDb.Card<DuchessEternalRestage>().Id))
    throw new Exception("Eternal Restage must remain eligible for Dusty Tome.");
foreach (string id in new[] { nameof(DuchessSoftLanding), nameof(DuchessDistraction) })
{
    var conceal = DuchessCardCatalog.All[id].Effects.Single(e => e.Kind == "Concealment");
    if (conceal.Amount != 2 || conceal.Upgraded != 3)
        throw new Exception(id + " must grant 2/3 Concealment.");
}
var trick = DuchessCardCatalog.All[nameof(DuchessSleightOfHand)];
if (!trick.All || trick.Effects.Single(e => e.Kind == "ShuffleHand") != new DuchessEffect("ShuffleHand", 1, 1)
    || trick.Effects.Single(e => e.Kind == "Weak").Amount != 1 || trick.Effects.Single(e => e.Kind == "Weak").Upgraded != 2
    || trick.Effects.Single(e => e.Kind == "Vulnerable").Amount != 1 || trick.Effects.Single(e => e.Kind == "Vulnerable").Upgraded != 2)
    throw new Exception("Sleight of Hand must shuffle 1 and apply 1/2 Weak and Vulnerable to ALL enemies.");
var quiet = DuchessCardCatalog.All[nameof(DuchessQuietude)];
var parallel = DuchessCardCatalog.All[nameof(DuchessParallelTime)];
if (quiet.Cost != 0 || quiet.Rarity != CardRarity.Common || quiet.Type != CardType.Skill
    || quiet.Effects[0] != new DuchessEffect("ShuffleHand", 1, 1) || quiet.Effects[1] != new DuchessEffect("Concealment", 1, 2)
    || parallel.Cost != 0 || parallel.Rarity != CardRarity.Uncommon || parallel.Type != CardType.Skill || parallel.Moment != 3
    || !parallel.UpgradeRetain || parallel.Effects[0] != new DuchessEffect("SetMoment", 0, 0) || parallel.Effects[1] != new DuchessEffect("Draw", 2, 2, "moment"))
    throw new Exception("Quietude or Parallel Time specification differs from the supplied table.");
if (ModelDb.Card<DuchessQuietude>().TargetType != TargetType.Self || ModelDb.Card<DuchessParallelTime>().TargetType != TargetType.Self)
    throw new Exception("Both new skills must not require an enemy target.");
var piercerUpgrade = ModelDb.Card<DuchessCarianPiercer>().ToMutable();
if (piercerUpgrade.DynamicVars.Damage.BaseValue != 11
    || piercerUpgrade.DynamicVars["DrawReactionDamageBoost"].BaseValue != 8)
    throw new Exception("Carian Piercer must start at 11 damage and gain 8 damage on draw.");
MegaCrit.Sts2.Core.Commands.CardCmd.Upgrade(piercerUpgrade);
if (piercerUpgrade.DynamicVars.Damage.BaseValue != 15
    || piercerUpgrade.DynamicVars["DrawReactionDamageBoost"].BaseValue != 8
    || piercerUpgrade.DynamicVars["DrawReactionDamageBoost"].WasJustUpgraded)
    throw new Exception("Carian Piercer upgrade must change only its base damage to 15.");
foreach (string blade in new[] { nameof(DuchessGoldenBlade), nameof(DuchessDeathBlade) })
{
    var spec = DuchessCardCatalog.All[blade];
    if (spec.Moment != 7 || spec.UpgradedMoment != 5)
        throw new Exception(blade + " must upgrade its required Moment from 7 to 5.");
}
var parallelUpgrade = ModelDb.Card<DuchessParallelTime>().ToMutable();
MegaCrit.Sts2.Core.Commands.CardCmd.Upgrade(parallelUpgrade);
if (parallelUpgrade.DynamicVars["Draw"].BaseValue != 2 || !parallelUpgrade.Keywords.Contains(CardKeyword.Retain))
    throw new Exception("Parallel Time upgrade must add Retain while still drawing 2 cards.");
var newCards = new[] { nameof(DuchessMemory), nameof(DuchessInchVictory), nameof(DuchessLorettaSlash),
    nameof(DuchessSacredHalo), nameof(DuchessGracefulSwordDance), nameof(DuchessMemoryFragment),
    nameof(DuchessPhantomKiller), nameof(DuchessGreatCaria), nameof(DuchessFate) };
if (newCards.Any(id => !DuchessCardCatalog.All.ContainsKey(id)))
    throw new Exception("A card from the new nine-card table is missing.");
var lorettaSlash = DuchessCardCatalog.All[nameof(DuchessLorettaSlash)];
if (lorettaSlash.Cost != 5 || !lorettaSlash.All || !lorettaSlash.MomentCostReductionDynamic
    || lorettaSlash.Effects[0] != new DuchessEffect("Damage", 11, 14))
    throw new Exception("Loretta's Slash must use native AOE and Moment-based energy reduction.");
var greatCaria = DuchessCardCatalog.All[nameof(DuchessGreatCaria)];
if (!greatCaria.All || !greatCaria.Reaction || greatCaria.Effects[0] != new DuchessEffect("Damage", 24, 32))
    throw new Exception("Great Caria must be Reaction AOE for 24/32 damage.");
var sacredHalo = ModelDb.Card<DuchessSacredHalo>().ToMutable();
if (sacredHalo.Keywords.Contains(CardKeyword.Retain)) throw new Exception("Sacred Halo must not retain before upgrade.");
sacredHalo.UpgradeInternal();
if (!sacredHalo.Keywords.Contains(CardKeyword.Retain)) throw new Exception("Sacred Halo upgrade must add Retain.");
var fate = ModelDb.Card<DuchessFate>().ToMutable();
if (!fate.Keywords.Contains(CardKeyword.Exhaust)) throw new Exception("Fate must exhaust before upgrade.");
fate.UpgradeInternal();
if (fate.Keywords.Contains(CardKeyword.Exhaust)) throw new Exception("Fate upgrade must remove Exhaust.");
if (DuchessCardCatalog.All[nameof(DuchessMemoryFragment)].Moment != 5
    || DuchessCardCatalog.All[nameof(DuchessMemoryFragment)].Effects[1].Kind != "ReturnSelfToHand"
    || DuchessCardCatalog.All[nameof(DuchessMemoryFragment)].Effects[1].Condition != "")
    throw new Exception("Memory Fragment must return on Moment arrival, not when played at Moment 5.");
var greatbow = DuchessCardCatalog.All[nameof(DuchessLorettaGreatbow)];
if (greatbow.Effects[0] != new DuchessEffect("Damage", 18, 22))
    throw new Exception("Loretta's Greatbow must deal 18 damage, upgraded to 22.");
var recollection = ModelDb.Card<DuchessMemory>().ToMutable();
if (recollection.DynamicVars["Energy"] is not EnergyVar
    || recollection.DynamicVars["Energy"].BaseValue != 1)
    throw new Exception("Recollection must use a 1-energy EnergyVar for its icon.");
recollection.UpgradeInternal();
if (recollection.DynamicVars["Energy"].BaseValue != 2)
    throw new Exception("Upgraded Recollection must use a 2-energy EnergyVar for its icon.");
if (instant.TargetType != TargetType.AnyEnemy || instant.DynamicVars["ExtraDamage"].BaseValue != 3
    || !instant.DynamicVars.ContainsKey("CalculatedDamage")
    || instant.DynamicVars["CalculationBase"].BaseValue != 6
    || instant.DynamicVars.CalculatedDamage.Calculate(null) != 6)
    throw new Exception("Fleeting Instant must target an enemy and preview one accumulated attack.");
instant.UpgradeInternal();
if (instant.DynamicVars["ExtraDamage"].BaseValue != 4)
    throw new Exception("Fleeting Instant upgrade must accumulate 4 damage per step.");
foreach (int stepCount in new[] { 0, 1, 12 })
{
    rewindMoment.SetAmount(stepCount + 1, false);
    if (instant.DynamicVars["CalculationBase"].BaseValue
        + instant.DynamicVars["ExtraDamage"].BaseValue * rewindMultiplier(instant, null) != 6 + 4 * stepCount)
        throw new Exception("Upgraded Fleeting Instant must preview 6 + 4 damage per rewind step.");
}
var escape = ModelDb.Card<DuchessEscape>().ToMutable();
if (DuchessCardCatalog.All.ContainsKey("DuchessPerfectRehearsal")
    || escape.EnergyCost.Canonical != 0 || escape.Type != CardType.Skill
    || escape.Rarity != CardRarity.Uncommon || escape.TargetType != TargetType.Self
    || escape.DynamicVars["LoseConcealment"].BaseValue != 2
    || escape.DynamicVars["ExhaustHandUpTo"].BaseValue != 2
    || escape.DynamicVars.Block.BaseValue != 4 || !escape.GainsBlock
    || escape.Keywords.Contains(CardKeyword.Exhaust))
    throw new Exception("Escape must replace Perfect Rehearsal: 0-cost skill, lose 2 Concealment, exhaust up to 2 hand cards.");
escape.Owner = rewindPlayer;
var escapeConcealment = (DuchessConcealmentPower)ModelDb.Power<DuchessConcealmentPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(escapeConcealment, rewindCreature);
var escapePowers = (List<PowerModel>)typeof(Creature).GetField("_powers",
    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(rewindCreature)!;
var escapePlayable = typeof(DuchessCard).GetProperty("IsPlayable",
    BindingFlags.Instance | BindingFlags.NonPublic)!;
bool EscapeIsPlayable() => (bool)escapePlayable.GetValue(escape)!;
if (EscapeIsPlayable()) throw new Exception("Escape must not be playable without Concealment.");
escapePowers.Add(escapeConcealment);
foreach (int stacks in new[] { 0, 1, 2, 3 })
{
    escapeConcealment.SetAmount(stacks, false);
    if (EscapeIsPlayable() != (stacks >= 2))
        throw new Exception($"Base Escape must require 2 Concealment; tested {stacks}.");
}
escape.UpgradeInternal();
if (escape.DynamicVars["LoseConcealment"].BaseValue != 1
    || escape.DynamicVars["ExhaustHandUpTo"].BaseValue != 2
    || escape.DynamicVars.Block.BaseValue != 4 || !escape.GainsBlock)
    throw new Exception("Escape upgrade must only reduce Concealment lost to 1.");
foreach (int stacks in new[] { 0, 1, 2, 3 })
{
    escapeConcealment.SetAmount(stacks, false);
    if (EscapeIsPlayable() != (stacks >= 1))
        throw new Exception($"Upgraded Escape must require 1 Concealment; tested {stacks}.");
}
escapePowers.Remove(escapeConcealment);
Console.WriteLine("PASS: Escape requires sufficient Concealment before play, base and upgraded.");
foreach (CardModel retained in new[] { ModelDb.Card<DuchessReenactment>().ToMutable(), ModelDb.Card<DuchessParallelTime>().ToMutable() })
{
    int cost = retained.EnergyCost.Canonical;
    if (retained.Keywords.Contains(CardKeyword.Retain)) throw new Exception("Retain must be upgrade-only.");
    retained.UpgradeInternal();
    if (!retained.Keywords.Contains(CardKeyword.Retain) || retained.EnergyCost.Canonical != cost)
        throw new Exception("Restage and Parallel Time upgrades must add Retain without reducing cost.");
}
var collar = ModelDb.Card<DuchessTidyCollar>().ToMutable();
if (collar.DynamicVars["ShuffleHand"].BaseValue != 1 || collar.DynamicVars["Draw"].BaseValue != 1)
    throw new Exception("Tidy Collar must shuffle and draw 1 card.");
collar.UpgradeInternal();
if (collar.DynamicVars["ShuffleHand"].BaseValue != 2 || collar.DynamicVars["Draw"].BaseValue != 2)
    throw new Exception("Tidy Collar upgrade must shuffle and draw 2 cards.");
var invisible = ModelDb.Card<DuchessBecomeInvisible>().ToMutable();
if (invisible.DynamicVars["Concealment"].BaseValue != 3) throw new Exception("Unseen Form must gain 3 Concealment.");
invisible.UpgradeInternal();
if (invisible.DynamicVars["Concealment"].BaseValue != 4) throw new Exception("Unseen Form upgrade must gain 4 Concealment.");
var newArsenal = ModelDb.Card<DuchessThiefsArsenal>().ToMutable();
if (!newArsenal.Keywords.Contains(CardKeyword.Exhaust)) throw new Exception("Thief's Arsenal must start with Exhaust.");
newArsenal.UpgradeInternal();
if (newArsenal.Keywords.Contains(CardKeyword.Exhaust) || newArsenal.EnergyCost.Canonical != 0)
    throw new Exception("Thief's Arsenal upgrade must remove Exhaust and remain zero-cost.");
if (DuchessCardCatalog.All[nameof(DuchessBackToPast)].Rarity != CardRarity.Common
    || DuchessCardCatalog.All[nameof(DuchessLorettaMastery)].Cost != 9
    || DuchessCardCatalog.All[nameof(DuchessLorettaMastery)].Hits != 3
    || DuchessCardCatalog.All[nameof(DuchessLorettaMastery)].Effects[0].Amount != 10
    || DuchessCardCatalog.All[nameof(DuchessLorettaGreatbow)].Cost != 5
    || DuchessCardCatalog.All[nameof(DuchessSilverFlash)].Effects[1] != new DuchessEffect("ConcealedBonusDamage", 10, 14)
    || DuchessCardCatalog.All[nameof(DuchessCarianRetaliation)].Effects[0] != new DuchessEffect("Block", 6, 9)
    || DuchessCardCatalog.All[nameof(DuchessGoldenMoment)].Effects[0] != new DuchessEffect("MomentFiveBlock", 5, 7))
    throw new Exception("October Duchess balance values do not match the requested changes.");
if (typeof(DuchessBeatPower).GetMethod("OnMomentSix") == null
    || typeof(DuchessBeatPower).GetMethod("OnMomentFive") != null
    || typeof(DuchessBeatPower).GetProperty("TriggeredThisTurn") != null
    || typeof(DuchessMomentFiveBlockPower).GetMethod("OnMomentThree") == null
    || typeof(DuchessMomentFiveRewardPower).IsAssignableFrom(typeof(DuchessMomentFiveBlockPower)))
    throw new Exception("Beat must reward every arrival at 6; Golden Moment must reward arrival at 3, not 5.");
foreach (var retainCard in new CardModel[] { ModelDb.Card<DuchessMomentAndEternity>().ToMutable(), ModelDb.Card<DuchessEternalForm>().ToMutable() })
{
    if (retainCard.Keywords.Contains(CardKeyword.Retain)) throw new Exception("Base new card must not Retain.");
    retainCard.UpgradeInternal();
    if (!retainCard.Keywords.Contains(CardKeyword.Retain)) throw new Exception("New card upgrade must add Retain.");
}
var shadow = ModelDb.Card<DuchessShadowSword>().ToMutable();
if (shadow.DynamicVars["ConcealedStrength"].BaseValue != 2) throw new Exception("Shadow Sword base Strength must be 2.");
shadow.UpgradeInternal();
if (shadow.DynamicVars["ConcealedStrength"].BaseValue != 3) throw new Exception("Shadow Sword upgraded Strength must be 3.");
foreach (string name in new[] { nameof(DuchessRestage), nameof(DuchessReenactment), nameof(DuchessFleetingInstant), nameof(DuchessGoldenBlade), nameof(DuchessSilverFlash), nameof(DuchessSilence) })
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
var silverStorm = (DuchessSilverStorm)ModelDb.Card<DuchessSilverStorm>().ToMutable();
silverStorm.Owner = rewindPlayer;
var stormConcealment = (DuchessConcealmentPower)ModelDb.Power<DuchessConcealmentPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(stormConcealment, rewindCreature);
stormConcealment.SetAmount(1, false);
var stormPowers = (List<PowerModel>)typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)!
    .GetValue(rewindCreature)!;
foreach (bool upgradedStorm in new[] { false, true })
{
    if (upgradedStorm) silverStorm.UpgradeInternal();
    rewindMoment.TryModifyEnergyCostInCombat(silverStorm, 3m, out decimal normalStormCost);
    stormPowers.Add(stormConcealment);
    rewindMoment.TryModifyEnergyCostInCombat(silverStorm, 3m, out decimal concealedStormCost);
    stormPowers.Remove(stormConcealment);
    if (normalStormCost != 3m || concealedStormCost != 2m)
        throw new Exception("Silver Storm must cost 3 normally and 2 while Concealed, base and upgraded.");
}
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
    || bladeReveal.Effects[0] != new DuchessEffect("Damage", 7, 7)
    || bladeReveal.Effects[1] != new DuchessEffect("Draw", 2, 3, "moment"))
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
if (reverberation.Rarity != CardRarity.Uncommon || reverberation.Effects.Length != 2 || reverberation.UpgradeCost != -1
    || reverberation.Effects[0] != new DuchessEffect("ShuffleDiscard", 2, 2)
    || reverberation.Effects[1] != new DuchessEffect("Draw", 2, 3))
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
    || swayingStep.Effects.Length != 1 || swayingStep.Effects[0] != new DuchessEffect("TransformDrawToDodge", 2, 2)
    || !swayingStep.UpgradeTokens)
    throw new Exception("Swaying Step specification is wrong.");
var insightFuture = DuchessCardCatalog.All[nameof(DuchessInsightFuture)];
if (insightFuture.Cost != 1 || insightFuture.Type != CardType.Skill || insightFuture.Rarity != CardRarity.Common
    || insightFuture.Effects.Length != 2 || insightFuture.Effects[0] != new DuchessEffect("Block", 4, 4)
    || insightFuture.Effects[1] != new DuchessEffect("ChooseDrawToTop", 1, 1)
    || !insightFuture.Exhaust || !insightFuture.UpgradeRemoveExhaust
    || ModelDb.Card<DuchessInsightFuture>().ToMutable().TargetType != TargetType.Self)
    throw new Exception("Insight Future specification is wrong.");

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
if (magicRadiantBlade.Cost != 0 || magicRadiantBlade.Type != CardType.Skill
    || magicRadiantBlade.Rarity != CardRarity.Uncommon || !magicRadiantBlade.XCost
    || !magicRadiantBlade.UpgradeX || !magicRadiantBlade.TargetSelf
    || magicRadiantBlade.Effects.Length != 1 || magicRadiantBlade.Effects[0].Kind != "RadiantBladeTurns")
    throw new Exception("Magic Radiant Blade specification is wrong.");

var radiantBladeArray = DuchessCardCatalog.All[nameof(DuchessRadiantBladeArray)];
if (radiantBladeArray.Cost != 1 || radiantBladeArray.Type != CardType.Skill
    || radiantBladeArray.Rarity != CardRarity.Common || !radiantBladeArray.TargetSelf
    || !radiantBladeArray.UpgradeTokens || radiantBladeArray.Effects.Length != 1
    || radiantBladeArray.Effects[0] != new DuchessEffect("RadiantBladeToDraw", 3, 3))
    throw new Exception("Radiant Blade Array specification is wrong.");
var cariaPhalanx = DuchessCardCatalog.All[nameof(DuchessCariaPhalanx)];
if (cariaPhalanx.Cost != 2 || cariaPhalanx.Type != CardType.Skill
    || cariaPhalanx.Rarity != CardRarity.Uncommon || !cariaPhalanx.Reaction
    || !cariaPhalanx.UpgradeTokens || !cariaPhalanx.TargetSelf
    || cariaPhalanx.Effects.Length != 1
    || cariaPhalanx.Effects[0] != new DuchessEffect("TransformDrawToRadiantBlade", 2, 2))
    throw new Exception("Caria Phalanx specification is wrong.");
foreach (CardModel skill in new CardModel[] { ModelDb.Card<DuchessMagicRadiantBlade>().ToMutable(),
             ModelDb.Card<DuchessRadiantBladeArray>().ToMutable(), ModelDb.Card<DuchessCariaPhalanx>().ToMutable(),
             ModelDb.Card<DuchessGreatswordPhalanx>().ToMutable() })
    if (skill.Type != CardType.Skill || skill.TargetType != TargetType.Self)
        throw new Exception(skill.Id + " must be a self-targeted Skill.");
var angelWings = DuchessCardCatalog.All[nameof(DuchessAngelWings)];
if (angelWings.Cost != 2 || angelWings.Type != CardType.Attack
    || angelWings.Rarity != CardRarity.Rare || angelWings.Effects.Length != 2
    || angelWings.Effects[0] != new DuchessEffect("Damage", 10, 10)
    || angelWings.Effects[1] != new DuchessEffect("ShuffleHandDamage", 6, 9))
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
if (arsenal.Cost != 0 || arsenal.UpgradeCost != -1 || !arsenal.Exhaust || !arsenal.UpgradeRemoveExhaust
    || arsenal.Effects.Length != 1 || arsenal.Effects[0].Kind != "DrawUntilMomentHandSize")
    throw new Exception("Thief's Arsenal specification is wrong.");
if (DuchessCardCatalog.All[nameof(DuchessMomentAndEternity)].Effects.Single().Kind != "RememberMoment"
    || typeof(DuchessMomentPower).GetProperty(nameof(DuchessMomentPower.SkipNextTurnReset)) == null)
    throw new Exception("Moment and Eternity must skip the next turn's Moment reset.");
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
    formatter.AddExtensions(new HighlightDifferencesFormatter(), new ShowIfUpgradedFormatter(), new ConditionalFormatter());
    var bearingPreviewCard = ModelDb.Card<DuchessElegantBearing>().ToMutable();
    string renderedBase = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, baseText,
        new Dictionary<string, object> { ["DodgeToDraw"] = bearingPreviewCard.DynamicVars["DodgeToDraw"], ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.Normal) });
    string renderedPreview = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, baseText,
        new Dictionary<string, object> { ["DodgeToDraw"] = bearingPreviewCard.DynamicVars["DodgeToDraw"], ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.UpgradePreview) });
    if (renderedBase.Contains("闪避+") || !renderedPreview.Contains("闪避[green]+[/green]"))
        throw new Exception("The game's formatter must visibly change Elegant Bearing's generated Dodge on upgrade preview.");
    string haloText = cards.RootElement.GetProperty("DUCHESS_MIQUELLAS_HALO.description").GetString()!;
    if (!haloText.Contains("造成等同于当前[gold]时刻[/gold]乘2的伤害X{IfUpgraded:show:+1|}次。{InCombat:")
        || haloText.Contains("抽牌堆") || haloText.Contains("时刻7"))
        throw new Exception("Miquella's Halo must preview Moment damage with X/X+1 hits.");
    var halo = ModelDb.Card<DuchessMiquellasHalo>().ToMutable();
    if (!DuchessCardCatalog.All[nameof(DuchessMiquellasHalo)].XCost
        || !DuchessCardCatalog.All[nameof(DuchessMiquellasHalo)].UpgradeX
        || DuchessCardCatalog.All[nameof(DuchessMiquellasHalo)].Cost != 0
        || !halo.DynamicVars.ContainsKey("CalculatedDamage")
        || halo.DynamicVars["ExtraDamage"].BaseValue != 2)
        throw new Exception("Miquella's Halo must be an X-cost attack with X+1 hit upgrade.");
    _ = halo.DynamicVars.CalculatedDamage.Calculate(null);
    string haloBase = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, haloText,
        new Dictionary<string, object> { ["InCombat"] = false, ["CalculatedDamage"] = halo.DynamicVars.CalculatedDamage, ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.Normal) });
    string haloPreview = formatter.Format(System.Globalization.CultureInfo.InvariantCulture, haloText,
        new Dictionary<string, object> { ["InCombat"] = false, ["CalculatedDamage"] = halo.DynamicVars.CalculatedDamage, ["IfUpgraded"] = new IfUpgradedVar(UpgradeDisplay.UpgradePreview) });
    if (!haloBase.Contains("X次") || !haloPreview.Contains("X[green]+1[/green]次") || haloBase == haloPreview)
        throw new Exception("Miquella's Halo upgrade preview must visibly change X to X+1.");
    if (haloBase.Contains("（") || haloPreview.Contains("（"))
        throw new Exception("Moment damage must not show a calculated zero in library or upgrade preview.");
    string phalanxBase = cards.RootElement.GetProperty("DUCHESS_CARIA_PHALANX.description").GetString()!;
    string phalanxUpgrade = cards.RootElement.GetProperty("DUCHESS_CARIA_PHALANX.upgradeDescription").GetString()!;
    if (!phalanxBase.Contains("辉剑") || phalanxBase.Contains("辉剑+") || !phalanxUpgrade.Contains("辉剑+"))
        throw new Exception("Caria Phalanx must preview Radiant Blade before upgrade and Radiant Blade+ after upgrade.");
    string angelBase = cards.RootElement.GetProperty("DUCHESS_ANGEL_WINGS.description").GetString()!;
    string angelUpgrade = cards.RootElement.GetProperty("DUCHESS_ANGEL_WINGS.upgradeDescription").GetString()!;
    if (!angelBase.Contains("伤害增加{ShuffleHandDamage:diff()}")
        || !angelUpgrade.Contains("伤害增加{ShuffleHandDamage:diff()}"))
        throw new Exception("Angel Wings must bind its visible shuffle-growth amount to the upgraded dynamic variable.");
    foreach (string cardId in new[] { "ANGEL_WINGS", "GOLDEN_BLADE", "CARIAN_PIERCER", "GLINTSTONE_HAIL" })
    {
        foreach (string suffix in new[] { "description", "upgradeDescription" })
        {
            string description = cards.RootElement.GetProperty($"DUCHESS_{cardId}.{suffix}").GetString()!;
            if (description.Contains("伤害+") || !description.Contains("伤害增加"))
                throw new Exception($"{cardId} {suffix} must write damage increases in words.");
        }
    }
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
// Invoke the actual patched base hook, including the optional Beta CardPlay.
var damageCompatHarmony = new HarmonyLib.Harmony("NightMustStay.Duchess.DamageCompat.Tests");
foreach (string name in new[] { "DuchessConcealmentDamageBranchPatch", "DuchessZeroCostAttackDamageBranchPatch" })
    damageCompatHarmony.CreateClassProcessor(typeof(DuchessConcealmentPower).Assembly
        .GetType("NightMustStay.Core.Patches."+name)!).Patch();
decimal DamageHook(PowerModel power, string name, ValueProp props, Creature dealer, CardModel card)
{
    var method=typeof(PowerModel).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
        .Single(candidate => candidate.Name==name);
    object?[] args=method.GetParameters().Length==5
        ? new object?[] { null,10m,props,dealer,card }
        : new object?[] { null,10m,props,dealer,card,null };
    return (decimal)method.Invoke(power,args)!;
}
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

// Exercise the actual damage hook against native local cost modifiers.
var zeroCostBonus = (DuchessZeroCostAttackPower)ModelDb.Power<DuchessZeroCostAttackPower>().ToMutable();
var bonusDealer = (Creature)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Creature));
typeof(PowerModel).GetProperty("Owner")!.SetValue(zeroCostBonus, bonusDealer);
var concealment = (DuchessConcealmentPower)ModelDb.Power<DuchessConcealmentPower>().ToMutable();
typeof(PowerModel).GetProperty("Owner")!.SetValue(concealment, bonusDealer);
if (DamageHook(concealment,"ModifyDamageMultiplicative",ValueProp.Move,bonusDealer,null!) != 1.25m
    || DamageHook(concealment,"ModifyDamageMultiplicative",ValueProp.Unpowered,bonusDealer,null!) != 1m
    || DamageHook(concealment,"ModifyDamageMultiplicative",ValueProp.Move,null!,null!) != 1m)
    throw new Exception("Concealment must boost only the owner's powered attacks by 25%.");
zeroCostBonus.SetAmount(4, false);
var discountedAttack = ModelDb.Card<DuchessCarianSlicer>().ToMutable();
decimal BonusFor(CardModel candidate) => DamageHook(zeroCostBonus,"ModifyDamageAdditive",
    ValueProp.Move,bonusDealer,candidate);
if (discountedAttack.EnergyCost.Canonical != 1 || BonusFor(discountedAttack) != 0)
    throw new Exception("Inch Victory must not boost a positive-cost attack.");
discountedAttack.EnergyCost.AddUntilPlayed(-1, true);
if (discountedAttack.EnergyCost.GetResolved() != 0 || BonusFor(discountedAttack) != 4)
    throw new Exception("Inch Victory must boost an originally nonzero attack discounted to zero.");
var turnDiscountedAttack = ModelDb.Card<DuchessCarianGreatsword>().ToMutable();
turnDiscountedAttack.EnergyCost.SetThisTurn(0, true);
if (BonusFor(turnDiscountedAttack) != 4)
    throw new Exception("Inch Victory must include attacks set to zero for the turn.");
if (BonusFor(ModelDb.Card<DuchessDefend>().ToMutable()) != 0
    || DamageHook(zeroCostBonus,"ModifyDamageAdditive",ValueProp.Unpowered,bonusDealer,turnDiscountedAttack) != 0
    || DamageHook(zeroCostBonus,"ModifyDamageAdditive",ValueProp.Move,null!,turnDiscountedAttack) != 0)
    throw new Exception("Zero-cost bonus must exclude skills, unpowered damage and other dealers.");
Console.WriteLine("PASS: actual branch-compatible damage hooks, concealment and resolved zero-cost conditions.");

if (DuchessReactionRules.IsEligibleDraw(true, PileType.Hand)
    || !DuchessReactionRules.IsEligibleDraw(false, PileType.Hand)
    || DuchessReactionRules.IsEligibleDraw(false, PileType.Draw)
    || !DuchessReactionRules.IsDrawnIntoHand(PileType.Hand)
    || DuchessReactionRules.IsDrawnIntoHand(PileType.Draw))
    throw new Exception("Reaction must exclude only the native opening hand draw, not other draws.");
foreach (Type type in new[] { typeof(DuchessCard), typeof(DuchessReactionDrawPower),
             typeof(DuchessReactionDrawBlockPower) })
{
    if (type.GetMethod("AfterCardDrawn")?.DeclaringType != type)
        throw new Exception($"{type.Name} must use the native draw hook for Reaction effects.");
}

var harmony = new HarmonyLib.Harmony("NightMustStay.Duchess.Tests");
// Verify private-field injection against the actual installed UI API.
harmony.CreateClassProcessor(typeof(RevenantNecroActionLayoutPatch)).Patch();
foreach (Type patch in new[] { typeof(RevenantSpiritJarSavePatch), typeof(RevenantSpiritJarLoadPatch),
    typeof(RevenantSpiritJarHistoryPatch) }) harmony.CreateClassProcessor(patch).Patch();
foreach (var patch in typeof(NightMustStay.Core.Patches.DuchessMomentPatch).Assembly.GetTypes()
    .Where(t => t.Namespace == "NightMustStay.Core.Patches" && t.Name.StartsWith("Duchess")))
    harmony.CreateClassProcessor(patch).Patch();
var activate = HarmonyLib.AccessTools.Method(typeof(NCombatUi), nameof(NCombatUi.Activate));
var momentCounterPatch = typeof(NightMustStay.Core.Patches.DuchessAssetPatch)
    .GetMethod(nameof(NightMustStay.Core.Patches.DuchessAssetPatch.InitializeDuchessMomentCounter))!;
if (HarmonyLib.Harmony.GetPatchInfo(activate)?.Postfixes.Any(p => p.PatchMethod == momentCounterPatch) != true)
    throw new Exception("Duchess Moment UI must be attached after combat UI reparents the star counter.");
foreach (bool upgradedSlicer in new[] { false, true })
{
    var redrawnSlicer = (DuchessCarianSlicer)ModelDb.Card<DuchessCarianSlicer>().ToMutable();
    if (upgradedSlicer) redrawnSlicer.UpgradeInternal();
    var drawOwner = (Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Player));
    typeof(Player).GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(drawOwner, new Creature(drawOwner, 70, 70));
    var handFixture = new CardPile(PileType.Hand);
    typeof(Player).GetField("_runPiles", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(drawOwner, new[] { handFixture });
    redrawnSlicer.Owner = drawOwner;
    handFixture.AddInternal(redrawnSlicer, silent: true);
    var slicerPlay = (CardPlay)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
    typeof(CardPlay).GetProperty("Card")!.SetValue(slicerPlay, redrawnSlicer);
    redrawnSlicer.AfterCardDrawn(null!, redrawnSlicer, true).GetAwaiter().GetResult();
    if (redrawnSlicer.EnergyCost.GetResolved() != 1)
        throw new Exception("Opening hand draw must not discount Carian Slicer.");
    redrawnSlicer.AfterCardDrawn(null!, redrawnSlicer, false).GetAwaiter().GetResult();
    if (redrawnSlicer.EnergyCost.GetResolved() != 0)
        throw new Exception("Normal draw must discount Carian Slicer.");
    redrawnSlicer.BeforeCardPlayed(slicerPlay).GetAwaiter().GetResult();
    redrawnSlicer.EnergyCost.AfterCardPlayedCleanup();
    if (redrawnSlicer.EnergyCost.GetResolved() != 1)
        throw new Exception("Playing Carian Slicer must consume the existing discount.");
    redrawnSlicer.BeforeCardPlayed(slicerPlay).GetAwaiter().GetResult();
    // Pocketwatch draws the same physical card during its prior play's late hook.
    redrawnSlicer.AfterCardDrawn(null!, redrawnSlicer, false).GetAwaiter().GetResult();
    redrawnSlicer.EnergyCost.AfterCardPlayedCleanup();
    if (redrawnSlicer.EnergyCost.GetResolved() != 0)
        throw new Exception("Pocketwatch redraw discount must survive cleanup of the previous play.");
    redrawnSlicer.BeforeCardPlayed(slicerPlay).GetAwaiter().GetResult();
    redrawnSlicer.EnergyCost.AfterCardPlayedCleanup();
    if (redrawnSlicer.EnergyCost.GetResolved() != 1)
        throw new Exception("Redraw discount must expire on the next actual play, not last forever.");
}
Console.WriteLine("PASS: base/upgraded Carian Slicer redraw discounts survive old-play cleanup and expire on the next play.");
harmony.UnpatchAll(harmony.Id);
FrenziedThreeFingersRegression.Run();
Console.WriteLine("PASS: independent Moment, Reaction/Dodge core, starter loadout, and all Duchess patch bindings.");
return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

public static class WhirlingTitleFixture
{
    public static bool Prefix(CardModel __instance, ref string __result)
    {
        __result = __instance.Id.Entry;
        return false;
    }
}
