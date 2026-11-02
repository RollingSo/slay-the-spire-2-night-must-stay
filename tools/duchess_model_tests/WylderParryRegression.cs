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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Power;

// Called while the established battle-art fixture stubs UI commands. Intent,
// hand/pile identity, card callbacks, Retain and native targeting remain real.
internal static class WylderParryRegression
{
    private static readonly Type Shared = typeof(WylderBattleArtRegression);
    private static Player Owner => (Player)AccessTools.Field(Shared, "_owner").GetValue(null)!;
    private static CombatState Combat => (CombatState)AccessTools.Field(Shared, "_combat").GetValue(null)!;
    private static Creature[] Enemies => (Creature[])AccessTools.Field(Shared, "_enemies").GetValue(null)!;
    private static readonly List<(CardModel Card, Creature Target)> AutoPlays = new();
    private static List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)> Rewards =>
        (List<(string Kind, Creature? Target, decimal Amount, CardPlay? Play)>)AccessTools.Field(Shared, "Rewards").GetValue(null)!;
    private static int _checks;

    internal static void Run()
    {
        if (!ModelDb.Contains(typeof(BigDummy))) AccessTools.Method(typeof(ModelDb), "Inject")!.Invoke(null, new object[] { typeof(BigDummy) });
        var h = new HarmonyLib.Harmony("NightMustStay.Wylder.Buckler.Regression");
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderParryTargetPatch")!).Patch();
        h.Patch(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.AutoPlay)), prefix: new HarmonyMethod(typeof(WylderParryRegression), nameof(AutoPlay)));
        h.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Stun), new[] { typeof(Creature), typeof(string) }),
            prefix: new HarmonyMethod(typeof(WylderParryRegression), nameof(MarkStunned)) { priority = Priority.First });
        try
        {
            foreach (bool upgraded in new[] { false, true })
            foreach (bool pursuit in new[] { false, true })
            {
                Fixture(pursuit);
                var parry = Card(upgraded);
                Put(parry, PileType.Hand);
                Check(parry.EnergyCost.GetResolved() == 4 && parry.Type == CardType.Skill && parry.Rarity == CardRarity.Common, "4-cost Common Skill base/upgraded");
                Check(parry.Keywords.Contains(CardKeyword.Retain) == upgraded && parry.ShouldRetainThisTurn == upgraded, "Upgrade adds real native Retain");
                Check(!parry.HasTurnEndInHandEffect, "Do not use native in-hand wrapper that moves/discards before this condition");
                Check(parry.IsValidTarget(Enemies[0]) && !parry.IsValidTarget(Enemies[1]) && !parry.IsValidTarget(Owner.Creature), "Only living attacking enemies are manually targetable");
                Owner.PlayerCombatState!.Energy = 0;
                End(parry);
                Check(AutoPlays.Count == 1 && AutoPlays[0].Card == parry && AutoPlays[0].Target == Enemies[0], "Sole hand card queues native AutoPlay even with no energy");
                Check(Enemies[0].IsStunned && !Enemies[1].IsStunned, "AutoPlay stuns only selected attacking enemy");
                Check(Owner.PlayerCombatState.Energy == 0 && !Owner.Creature.HasPower<WylderPursuitPower>(), "Automatic Skill spends no energy and consumes prior Pursuit");
                Check(Rewards.Count(reward => reward.Kind == "Draw") == (pursuit ? 1 : 0), "Automatic Skill receives Pursuit's normal one-card draw exactly once");
                Check(PileType.Hand.GetPile(Owner).Cards.Count == 0 && parry.Pile?.Type == PileType.Discard, "Played card leaves hand and reaches discard");
                End(parry);
                Check(AutoPlays.Count == 1, "No second automatic play from discard");

                Fixture(pursuit); parry = Card(upgraded); Put(parry, PileType.Hand);
                Put(Other(), PileType.Hand);
                End(parry);
                Check(AutoPlays.Count == 0 && !Enemies[0].IsStunned, "Two cards in hand cannot trigger Desperation");
                Check(parry.Pile?.Type == PileType.Hand && parry.ShouldRetainThisTurn == upgraded, "Non-triggered upgraded card remains eligible for native Retain");
                Check(Owner.Creature.HasPower<WylderPursuitPower>() == pursuit, "No trigger means no Pursuit consumption");

                Fixture(pursuit); parry = Card(upgraded); Put(parry, PileType.Hand);
                SetIntent(Enemies[0], false);
                End(parry);
                Check(AutoPlays.Count == 0 && parry.Pile?.Type == PileType.Hand, "No attacking target produces no empty AutoPlay/CardPlay");
                Check(Owner.Creature.HasPower<WylderPursuitPower>() == pursuit, "No valid target preserves Pursuit");

                Fixture(); parry = Card(upgraded); Put(parry, PileType.Hand);
                parry.BeforeSideTurnEnd(null!, CombatSide.Enemy, Enemies).GetAwaiter().GetResult();
                parry.BeforeSideTurnEnd(null!, CombatSide.Player, Array.Empty<Creature>()).GetAwaiter().GetResult();
                Check(AutoPlays.Count == 0, "Enemy turn end or another player's turn cannot trigger own card");

                Fixture(); parry = Card(upgraded); Put(parry, PileType.Hand);
                Enemies[0].SetCurrentHpInternal(0);
                Check(!parry.IsValidTarget(Enemies[0]), "Dead attacking enemy is invalid");
                End(parry);
                Check(AutoPlays.Count == 0, "Dead-only attack targets never queue AutoPlay");

                Fixture(); parry = Card(upgraded); Put(parry, PileType.Hand);
                SetIntent(Enemies[1], true); SetIntent(Enemies[0], false);
                End(parry);
                Check(AutoPlays.Single().Target == Enemies[1] && Enemies[1].IsStunned && !Enemies[0].IsStunned, "Target selection filters defending enemy instead of choosing an illegal one");
            }
            void HoverStub(MethodBase target, string stub) => h.Patch(target,
                prefix: new HarmonyMethod(typeof(CardGlossaryRegression), stub));
            HoverStub(AccessTools.Method(typeof(HoverTipFactory), nameof(HoverTipFactory.FromPower), new[] { typeof(PowerModel), typeof(int?) }), nameof(CardGlossaryRegression.Power));
            HoverStub(AccessTools.Method(typeof(LocString), nameof(LocString.GetRawText)), nameof(CardGlossaryRegression.Raw));
            HoverStub(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText)), nameof(CardGlossaryRegression.Formatted));
            foreach (string locale in new[] { "zhs", "eng", "jpn", "kor" })
            {
                AccessTools.Method(typeof(CardGlossaryRegression), "Load").Invoke(null, new object[] { locale });
                using var json = JsonDocument.Parse(File.ReadAllText($"NightMustStay/localization/{locale}/cards.json"));
                string text = json.RootElement.GetProperty("WYLDER_PARRY.description").GetString()!;
                string term = locale == "zhs" ? "绝境" : "Desperation";
                Check(text.Contains("[gold]" + term + "[/gold]") && !text.Contains("Retain") && !text.Contains("保留"), "Gold Desperation, native Retain not duplicated");
                Check(text == json.RootElement.GetProperty("WYLDER_PARRY.upgradeDescription").GetString(), "Keyword-only upgrade audit body synced");
                Check(json.RootElement.GetProperty("WYLDER_DESPERATION.title").GetString() == term, "Desperation extra tip title localized");
                Check(json.RootElement.GetProperty("WYLDER_DESPERATION.description").GetString()!.Length > 15, "Desperation extra explanation is present");
                foreach (bool upgraded in new[] { false, true })
                {
                    var tips = ((IEnumerable<IHoverTip>)AccessTools.PropertyGetter(typeof(WylderParry), "ExtraHoverTips")
                        .Invoke(Card(upgraded), null)!).OfType<HoverTip>().Where(tip => tip.Title == term).ToArray();
                    Check(tips.Length == 1 && tips[0].Description == json.RootElement.GetProperty("WYLDER_DESPERATION.description").GetString(),
                        "Base/upgraded card really exposes exactly one localized Desperation explanation");
                }
            }
            Check(ModelDb.Card<WylderParry>().PortraitPath == "res://images/packed/card_portraits/wylder/wylder_parry.png", "Buckler custom portrait path");
            Console.WriteLine($"PASS: {_checks} Buckler Parry checks: sole-hand native AutoPlay, intent/target/turn filtering, zero energy, Pursuit, upgrade Retain and four-locale Desperation text.");
        }
        finally { h.UnpatchAll(h.Id); }
    }

    private static void Fixture(bool pursuit = false)
    {
        AccessTools.Method(Shared, "Fixture").Invoke(null, new object[] { pursuit }); AutoPlays.Clear();
        var enemies = Enumerable.Range(0, 2).Select(_ => new Creature(ModelDb.Monster<BigDummy>().ToMutable(), CombatSide.Enemy, null) { CombatState = Combat }).ToArray();
        var list = (List<Creature>)AccessTools.Field(typeof(CombatState), "_enemies").GetValue(Combat)!;
        list.Clear(); list.AddRange(enemies); AccessTools.Field(Shared, "_enemies").SetValue(null, enemies);
        SetIntent(enemies[0], true); SetIntent(enemies[1], false);
    }
    private static void SetIntent(Creature enemy, bool attacking) => AccessTools.PropertySetter(typeof(MonsterModel), nameof(MonsterModel.NextMove))
        .Invoke(enemy.Monster, new object[] { new MoveState(attacking ? "ATTACK" : "DEFEND", _ => Task.CompletedTask,
            attacking ? new SingleAttackIntent(8) : new DefendIntent()) });
    private static CardModel Card(bool upgraded) => (CardModel)AccessTools.Method(Shared, "Card").Invoke(null, new object[] { ModelDb.Card<WylderParry>(), upgraded })!;
    private static CardModel Other() => (CardModel)AccessTools.Method(Shared, "Card").Invoke(null, new object[] { ModelDb.Card<WylderDefend>(), false })!;
    private static void Put(CardModel card, PileType pile) => AccessTools.Method(Shared, "Put").Invoke(null, new object[] { card, pile });
    private static void End(CardModel card) => card.BeforeSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
    public static bool AutoPlay(CardModel card, Creature target, ref Task __result)
    {
        if (card is not WylderParry) return true;
        Check(card.Pile?.Type == PileType.Hand && card.IsValidTarget(target), "AutoPlay receives original hand card and legal intent target");
        AutoPlays.Add((card, target));
        AccessTools.Method(Shared, "Play").Invoke(null, new object?[] { card, true, true, 0, null, target });
        Put(card, PileType.Discard);
        __result = Task.CompletedTask; return false;
    }
    public static void MarkStunned(Creature __0)
    {
        if (__0.Monster != null) AccessTools.PropertySetter(typeof(MonsterModel), nameof(MonsterModel.NextMove))
            .Invoke(__0.Monster, new object[] { new MoveState("STUNNED", _ => Task.CompletedTask, new HiddenIntent()) });
    }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception("Buckler Parry: " + reason); }
}
