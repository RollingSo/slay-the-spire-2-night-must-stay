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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

// Reuse the established native model fixture, replacing only UI commands.
// The new tests drive real card callbacks, calculated previews, native Vigor
// Before/AfterAttack, and the shared pending-event API (not card formula copies).
internal static class WylderCompletionBRegression
{
    private static readonly Type FixtureType = typeof(WylderBattleArtRegression);
    private static Player Owner => (Player)AccessTools.Field(FixtureType, "_owner").GetValue(null)!;
    private static Creature[] Enemies => (Creature[])AccessTools.Field(FixtureType, "_enemies").GetValue(null)!;
    private static List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)> Rewards =>
        (List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)>)AccessTools.Field(FixtureType, "Rewards").GetValue(null)!;
    private static readonly List<decimal> Damage = new();
    private static bool _fullyBlocked, _killTargets;
    private static int _checks;
    internal static Type[] CardTypes => new[] { typeof(WylderAirCleavingChase), typeof(WylderTakersFlames), typeof(WylderInfernalSlash),
        typeof(WylderAssaultDrop), typeof(WylderCounteroffensive), typeof(WylderFlameWard), typeof(WylderGrappleLeap),
        typeof(WylderForcedBreakout), typeof(WylderSpendReserves), typeof(WylderCliffStep), typeof(WylderNightRespite),
        typeof(WylderWindChase), typeof(WylderSprintAfterimage), typeof(WylderLastEmber), typeof(WylderAllInDuel), typeof(WylderLongNightOath) };

    public static void Run(bool takersFlamesOnly = false)
    {
        var harmony = new HarmonyLib.Harmony("NightMustStay.Wylder.CompletionB.Regression");
        void Shared(MethodBase method, string name) => harmony.Patch(method,
            prefix: new HarmonyMethod(FixtureType, name) { priority = Priority.Last });
        void Own(MethodBase method, string name) => harmony.Patch(method,
            prefix: new HarmonyMethod(typeof(WylderCompletionBRegression), name));
        foreach (string patch in new[] { "WylderPursuitDamageBranchPatch", "WylderPendingEnergyCostPatch", "WylderNativeVigorAttackScopePatch", "WylderNativeVigorConsumptionPatch" })
            harmony.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches." + patch)!).Patch();
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
            prefix: new HarmonyMethod(typeof(ReversalStepRegression), nameof(ReversalStepRegression.InCombat)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatEndingFixture)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)),
            prefix: new HarmonyMethod(typeof(MajestyRegression), nameof(MajestyRegression.CombatStateFixture)));
        Shared(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Pool)), "Pool");
        Shared(AccessTools.Method(typeof(CombatState), nameof(CombatState.IterateHookListeners)), "Listeners");
        Shared(AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy)), "Energy");
        Shared(typeof(CreatureCmd).GetMethods().Single(m => m.Name == "GainBlock" && m.GetParameters()[1].ParameterType == typeof(decimal)), "Block");
        Shared(typeof(CardPileCmd).GetMethods().Single(m => m.Name == "Draw" && m.GetParameters().Length == 4), "Draw");
        Shared(typeof(PowerCmd).GetMethods().Single(m => m.Name == "Apply" && !m.IsGenericMethod), "Apply");
        Shared(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount)), "Modify");
        Shared(typeof(PowerCmd).GetMethods().Single(m => m.Name == "Remove" && !m.IsGenericMethod), "Remove");
        Own(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Attack));
        Own(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Heal)), nameof(Heal));
        Own(typeof(CreatureCmd).GetMethods().Single(m => m.Name == "Stun" && m.GetParameters().Length == 2), nameof(Stun));
        try
        {
            Metadata();
            foreach (bool upgraded in new[] { false, true })
            {
                Fixture(); var chase = Card<WylderAirCleavingChase>(upgraded); Play(chase);
                Check(Sum("Block") == 8 && Sum("Stun") == 0, "P050 failure gets only Block");
                Fixture(); Add<VigorPower>(20); chase = Card<WylderAirCleavingChase>(upgraded); Play(chase);
                Check(Sum("Block") == 0 && Sum("Stun") == 1, "P050 successful Break stuns without fallback");
                Fixture(); var flames = Card<WylderTakersFlames>(upgraded); Play(flames);
                Check(Sum("Heal") == 0 && Damage.Count == 2, "P053 one native AOE, no Break no heal");
                Fixture(); Add<StrengthPower>(14); _fullyBlocked = true; flames = Card<WylderTakersFlames>(upgraded); Play(flames);
                Check(Sum("Heal") == (upgraded ? 8 : 6) && Rewards.Count(r => r.Kind == "Heal") == 1, "P053 pre-block Break heals once");
                Fixture(); Add<StrengthPower>(14); _killTargets = true; flames = Card<WylderTakersFlames>(upgraded); Play(flames);
                Check(Sum("Heal") == (upgraded ? 8 : 6) && Enemies.All(e => !e.IsAlive), "P053 successful lethal Break remains eligible");
                Rewards.Clear();
                AccessTools.Field(typeof(WylderTakersFlames), "_pendingBreakHeal").SetValue(flames, true);
                flames.AfterCombatEnd(null!).GetAwaiter().GetResult();
                flames.AfterCombatEnd(null!).GetAwaiter().GetResult();
                Check(Sum("Heal") == (upgraded ? 8 : 6) && Rewards.Count(r => r.Kind == "Heal") == 1,
                    "P053 combat-end fallback settles the pending heal exactly once");
                if (takersFlamesOnly) continue;
                foreach (int vigor in new[] { 0, 3, 4, 7 })
                foreach (bool hand in new[] { false, true })
                {
                    Fixture(); if (vigor > 0) Add<VigorPower>(vigor);
                    var slash = Card<WylderInfernalSlash>(upgraded); Put(slash, hand ? PileType.Hand : PileType.Draw);
                    slash.DynamicVars.CalculatedDamage.UpdateCardPreview(slash, CardPreviewMode.Normal, Enemies[0], true);
                    decimal expected = (upgraded ? 20 : 17) + vigor + (hand && vigor >= 4 ? 10 : 0);
                    Check(slash.DynamicVars.CalculatedDamage.PreviewValue == expected, "P054 native final preview uses hand/Vigor threshold");
                    Play(slash); Check(Damage.Single() == expected, "P054 actual calculated hit equals preview");
                }
                Fixture(); var drop = Card<WylderAssaultDrop>(upgraded); Play(drop);
                Check(Damage.Count == 4 && Sum("PhantomImbalancePower") == 0, "P055 AOE two native hits without false Break");
                Fixture(); Add<StrengthPower>(6); drop = Card<WylderAssaultDrop>(upgraded); Play(drop);
                Check(Sum("PhantomImbalancePower") == 0, "P055 two sub-threshold hits never accumulate into Break");
                Fixture(); Add<StrengthPower>(22); drop = Card<WylderAssaultDrop>(upgraded); Play(drop);
                Check(Sum("PhantomImbalancePower") == 2, "P055 every hit reaching Break awards each survivor once");
                foreach (int block in new[] { 0, 4, 5, 20, 35 })
                {
                    Fixture(true); Owner.Creature.GainBlockInternal(block); var counter = Card<WylderCounteroffensive>(upgraded);
                    counter.DynamicVars.CalculatedDamage.UpdateCardPreview(counter, CardPreviewMode.Normal, Enemies[0], true);
                    decimal expected = ((upgraded ? 19 : 15) + Math.Min(4, block / 5) * 3) * 1.5m;
                    Check(counter.DynamicVars.CalculatedDamage.PreviewValue == expected, "P056 capped current Block preview");
                    Play(counter); Check(Damage.Single() == Math.Floor(expected) && Sum("Block") == 7, "P056 actual damage then Pursuit Block");
                }
                foreach (int vigor in new[] { 0, 5, 6 })
                {
                    Fixture(); if (vigor > 0) Add<VigorPower>(vigor); Add<DexterityPower>(2);
                    var ward = Card<WylderFlameWard>(upgraded); Play(ward);
                    Check(Sum("Block") == vigor + 2, "P057 current Vigor plus native Dexterity once");
                    Rewards.Clear(); Play(Card<WylderStrike>());
                    Check(Damage.Single() == 6 + vigor && Owner.Creature.GetPowerAmount<VigorPower>() == vigor / 2, "P057 full damage, half native consumption rounded up");
                }
                foreach (int vigor in new[] { 0, 2, 3, 5, 6, 11 })
                {
                    Fixture(); if (vigor > 0) Add<VigorPower>(vigor); Play(Card<WylderSpendReserves>(upgraded));
                    int count = Math.Min(2, vigor / 3);
                    Check(Sum("Energy") == count && Owner.Creature.GetPowerAmount<VigorPower>() == vigor - count * 3, "P062 exact groups and cap");
                    Check(Sum("Block") == (upgraded ? 11 : 7), "P062 base/upgraded Block");
                    Fixture(); if (vigor > 0) Add<VigorPower>(vigor); Play(Card<WylderLastEmber>(upgraded));
                    Check(Sum("Heal") == (vigor >= 6 ? upgraded ? 7 : 5 : 0) && Owner.Creature.GetPowerAmount<VigorPower>() == (vigor >= 6 ? vigor - 6 : vigor), "P068 real loss threshold and heal");
                }
                foreach (int previous in new[] { 0, 1, 4, 7 })
                {
                    Fixture(); for (int i = 0; i < previous; i++) Play(Card<WylderStrike>());
                    Rewards.Clear(); Add<DexterityPower>(2); var afterimage = Card<WylderSprintAfterimage>(upgraded);
                    afterimage.DynamicVars.CalculatedBlock.UpdateCardPreview(afterimage, CardPreviewMode.Normal, null!, true);
                    int expected = (upgraded ? 9 : 6) + Math.Min(4, previous) * 3 + 2;
                    Check(afterimage.DynamicVars.CalculatedBlock.PreviewValue == expected, "P067 history-capped native Block preview");
                    Play(afterimage); Check(Sum("Block") == expected && Rewards.Count(r => r.Kind == "Block") == 1, "P067 one GainBlock applies modifiers once");
                }
                SourceAndPending(upgraded);
            }
            Console.WriteLine(takersFlamesOnly
                ? $"PASS: {_checks} Taker's Flames checks, including lethal Break and exactly-once combat-end healing."
                : $"PASS: {_checks} Wylder B-completion checks: 16 cards, reviewed upgrades and user-final Taker's Flames, calculated previews, native AOE/multihit/Break, Vigor consumption, hand sources, pending discounts and next-turn rewards (UI commands stubbed).");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Metadata()
    {
        using var text = JsonDocument.Parse(File.ReadAllText("design/wylder/completion_b_localization.json"));
        foreach (var type in CardTypes)
        foreach (bool upgraded in new[] { false, true })
        {
            Fixture(); var card = Card(type, upgraded);
            Check(text.RootElement.GetProperty("cards").TryGetProperty(card.Id.Entry, out _), "B source localization exists " + card.Id);
            Check(card.Rarity == (card is WylderLongNightOath ? CardRarity.Ancient : CardRarity.Rare), "B rarity " + card.Id);
            string relativePortrait = card.PortraitPath.StartsWith("res://", StringComparison.Ordinal)
                ? card.PortraitPath["res://".Length..] : card.PortraitPath;
            string projectPortrait = Path.Combine(Directory.GetCurrentDirectory(), relativePortrait);
            string extractedGamePortrait = Path.Combine("D:/STS2", relativePortrait);
            Check(File.Exists(projectPortrait) || File.Exists(extractedGamePortrait), "B test portrait exists " + card.Id);
            if (card is WylderTakersFlames or WylderAssaultDrop) Check(card.TargetType == TargetType.AllEnemies, "B native AOE metadata");
            if (card is WylderFlameWard) Check(card.Keywords.Contains(CardKeyword.Retain) == upgraded && card.Keywords.Contains(CardKeyword.Exhaust), "P057 upgrade native Retain and Exhaust");
            if (card is WylderNightRespite) Check(card.Keywords.Contains(CardKeyword.Retain) && card.Keywords.Contains(CardKeyword.Exhaust), "P065 native keywords");
            if (card is WylderTakersFlames) Check(card.EnergyCost.Canonical == 2 && card.DynamicVars.Damage.BaseValue == (upgraded ? 20 : 16) && card.DynamicVars["Heal"].BaseValue == (upgraded ? 8 : 6), "P053 exact user-final values");
        }
    }

    private static void SourceAndPending(bool upgraded)
    {
        foreach (bool hand in new[] { false, true })
        foreach (bool pursuit in new[] { false, true })
        {
            Fixture(pursuit); var respite = Card<WylderNightRespite>(upgraded); Put(respite, hand ? PileType.Hand : PileType.Draw);
            Play(respite); Check(Sum("Energy") == (hand && pursuit ? 1 : 0), "P065 refund needs true hand and old Pursuit");
        }
        Fixture(); var leap = Card<WylderGrappleLeap>(upgraded); Play(leap);
        var strike = Card<WylderAirCleavingChase>(); Put(strike, PileType.Hand);
        Check(strike.EnergyCost.GetResolved() == (upgraded ? 0 : 1), "P058 native preview pending discount");
        Play(Card<WylderGrappleLeap>()); Check(strike.EnergyCost.GetResolved() == (upgraded ? 0 : 1), "P058 same-name uses maximum");
        var outside = Card<WylderStrike>(); Play(outside, replay: true);
        Check(strike.EnergyCost.GetResolved() == (upgraded ? 0 : 1), "P058 outside replay cannot consume pending");
        Play(strike); Put(strike, PileType.Hand); Check(strike.EnergyCost.GetResolved() == 2, "P058 one hand attack spends pending");
        Fixture(); Play(Card<WylderWindChase>(upgraded)); var skill = Card<WylderDefend>(); Put(skill, PileType.Hand);
        Check(skill.EnergyCost.GetResolved() == 0, "P066 next hand skill discount");
        WylderCombatEvents.EndTurn(Owner); Check(skill.EnergyCost.GetResolved() == 1, "P066 pending expires turn end");
        Fixture(); Play(Card<WylderCliffStep>(upgraded)); var ordinary = Card<WylderStrike>(); Put(ordinary, PileType.Hand);
        Check(ordinary.EnergyCost.GetResolved() == 1, "P063 never discounts ordinary attack");
        var breaking = Card<WylderTakersFlames>(true); Put(breaking, PileType.Hand);
        Check(breaking.EnergyCost.GetResolved() == 0, "P063 discounts hand Break attack");
        Add<StrengthPower>(10); Rewards.Clear(); Play(breaking);
        Check(Sum("Draw") == (upgraded ? 2 : 1), "P063 successful pending Break draws after attack");
        Fixture(); var oath = Card<WylderLongNightOath>(upgraded); Play(oath);
        Check(Owner.Creature.GetPowerAmount<VigorPower>() == 0, "P075 no early Vigor");
        WylderCombatEvents.StartTurn(Owner); WylderCombatEvents.PayStartTurn(null!, Owner).GetAwaiter().GetResult();
        Check(Owner.Creature.GetPowerAmount<VigorPower>() == (upgraded ? 6 : 4), "P075 pays next turn");
        WylderCombatEvents.PayStartTurn(null!, Owner).GetAwaiter().GetResult();
        Check(Owner.Creature.GetPowerAmount<VigorPower>() == (upgraded ? 6 : 4), "P075 pending pays once");
        foreach (int handCount in new[] { 2, 3, 4 })
        {
            Fixture(); var breakout = Card<WylderForcedBreakout>(upgraded); Put(breakout, PileType.Hand);
            var expensive = Card<WylderAssaultDrop>(); Put(expensive, PileType.Hand);
            for (int i = 2; i < handCount; i++) Put(Card<WylderDefend>(), PileType.Hand);
            Play(breakout); Check(expensive.EnergyCost.GetResolved() == Math.Max(0, 3 - (upgraded ? 2 : 1) - (handCount <= 3 ? 1 : 0)), "P061 highest-cost card and inclusive source snapshot");
            expensive.EnergyCost.EndOfTurnCleanup(); Check(expensive.EnergyCost.GetResolved() == 3, "P061 local native discount expiry");
            Fixture(); var duel = Card<WylderAllInDuel>(upgraded); Put(duel, PileType.Hand);
            expensive = Card<WylderAssaultDrop>(); Put(expensive, PileType.Hand);
            for (int i = 2; i < handCount; i++) Put(Card<WylderDefend>(), PileType.Hand);
            Play(duel); Check(expensive.EnergyCost.GetResolved() == (handCount <= 3 ? 1 : 2) && Sum("Block") == (upgraded ? 17 : 12), "P069 attacks discounted from source snapshot");
        }
    }

    private static void Fixture(bool pursuit = false)
    { AccessTools.Method(FixtureType, "Fixture").Invoke(null, new object[] { pursuit }); Damage.Clear(); _fullyBlocked = _killTargets = false; WylderCombatEvents.StartTurn(Owner); }
    private static CardModel Card(Type type, bool upgraded)
    {
        var model = (CardModel)typeof(ModelDb).GetMethods().Single(m => m.Name == "Card" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0).MakeGenericMethod(type).Invoke(null, null)!;
        return (CardModel)AccessTools.Method(FixtureType, "Card").Invoke(null, new object[] { model, upgraded })!;
    }
    private static CardModel Card<T>(bool upgraded = false) where T : CardModel => Card(typeof(T), upgraded);
    private static void Put(CardModel card, PileType pile) => AccessTools.Method(FixtureType, "Put").Invoke(null, new object[] { card, pile });
    private static void Add<T>(int amount) where T : PowerModel => AccessTools.Method(FixtureType, "AddPower").MakeGenericMethod(typeof(T)).Invoke(null, new object[] { Owner.Creature, amount });
    private static decimal Sum(string kind) => Rewards.Where(r => r.Kind == kind).Sum(r => r.Amount);
    private static void Check(bool condition, string label) { _checks++; if (!condition) throw new Exception("Wylder completion B: " + label); }
    private static void Play(CardModel card, bool replay = false)
    {
        var source = typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderHandPlayContext")!;
        AccessTools.Method(source, "CaptureWrapper").Invoke(null, new object[] { card });
        if (card.Pile?.Type == PileType.Hand) Put(card, PileType.Play);
        var play = TrackingArrowRegression.MakePlay(card, Owner);
        typeof(CardPlay).GetProperty(nameof(CardPlay.Target))!.SetValue(play, Enemies[0]);
        if (replay) { typeof(CardPlay).GetProperty(nameof(CardPlay.PlayIndex))!.SetValue(play, 1); typeof(CardPlay).GetProperty(nameof(CardPlay.PlayCount))!.SetValue(play, 2); }
        AccessTools.Method(source, "Record").Invoke(null, new object[] { play });
        card.BeforeCardPlayed(play).GetAwaiter().GetResult();
        foreach (var power in Owner.Creature.Powers.OfType<WylderPursuitPower>().ToArray()) power.BeforeCardPlayed(play).GetAwaiter().GetResult();
        CombatManager.Instance.History.CardPlayStarted(card.CombatState!, play);
        ((Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, new object?[] { null, play })!).GetAwaiter().GetResult();
        CombatManager.Instance.History.CardPlayFinished(card.CombatState!, play);
        card.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
        foreach (var power in Owner.Creature.Powers.OfType<WylderPursuitPower>().ToArray()) power.AfterCardPlayedLate(null!, play).GetAwaiter().GetResult();
        WylderCombatEvents.RegisterOldPursuitConsumption(play);
        WylderCombatEvents.CompleteCard(null!, play).GetAwaiter().GetResult();
    }
    public static bool Heal(Creature creature, decimal amount, ref Task __result)
    { Rewards.Add(("Heal", creature, amount, null)); __result = Task.CompletedTask; return false; }
    public static bool Stun(Creature creature, ref Task __result)
    { Rewards.Add(("Stun", creature, 1, null)); __result = Task.CompletedTask; return false; }
    public static bool Attack(AttackCommand __instance, PlayerChoiceContext choiceContext, ref Task<AttackCommand> __result)
    { __result = Resolve(__instance, choiceContext); return false; }
    private static async Task<AttackCommand> Resolve(AttackCommand attack, PlayerChoiceContext context)
    {
        var card = (CardModel)attack.ModelSource!;
        int hits = (int)AccessTools.Field(typeof(AttackCommand), "_hitCount").GetValue(attack)!;
        foreach (var power in Owner.Creature.Powers.ToArray()) await power.BeforeAttack(attack);
        for (int i = 0; i < hits; i++)
        foreach (var target in attack.IsMultiTargeted ? Enemies.Where(e => e.IsAlive).ToArray() : new[] { (Creature)AccessTools.Field(typeof(AttackCommand), "_singleTarget").GetValue(attack)! })
        {
            var damage = card.DynamicVars.TryGetValue("CalculatedDamage", out var calculated) ? calculated : card.DynamicVars.Damage;
            damage.UpdateCardPreview(card, CardPreviewMode.Normal, target, true);
            int amount = (int)Math.Floor(damage.PreviewValue); Damage.Add(amount);
            if (_killTargets) target.SetCurrentHpInternal(0);
            var result = new DamageResult(target, attack.DamageProps) { UnblockedDamage = _fullyBlocked ? 0 : amount, BlockedDamage = _fullyBlocked ? amount : 0 };
            await card.AfterDamageGiven(context, Owner.Creature, result, attack.DamageProps, target, card);
        }
        foreach (var power in Owner.Creature.Powers.ToArray()) await power.AfterAttack(context, attack);
        return attack;
    }
}
