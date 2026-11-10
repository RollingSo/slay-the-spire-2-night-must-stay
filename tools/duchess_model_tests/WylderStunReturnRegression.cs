using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;

internal static class WylderStunReturnRegression
{
    private static readonly Type Shared = typeof(WylderBattleArtRegression);
    private static Player Owner => (Player)AccessTools.Field(Shared, "_owner").GetValue(null)!;
    private static CombatState Combat => (CombatState)AccessTools.Field(Shared, "_combat").GetValue(null)!;
    private static readonly List<CardModel> Returned = new();
    private static int _checks;
    internal static void Run()
    {
        foreach (Type type in new[] { typeof(BigDummy) })
            if (!ModelDb.Contains(type)) AccessTools.Method(typeof(ModelDb), "Inject")!.Invoke(null, new object[] { type });
        var h = new HarmonyLib.Harmony("NightMustStay.Wylder.StunReturn.Regression");
        h.CreateClassProcessor(typeof(Wylder).Assembly.GetType("NightMustStay.Core.Patches.WylderStunReturnPatch")!).Patch();
        h.Patch(typeof(CardPileCmd).GetMethods().Single(method => method.Name == "Add"
            && method.GetParameters()[0].ParameterType == typeof(CardModel) && method.GetParameters()[1].ParameterType == typeof(PileType)),
            prefix: new HarmonyMethod(typeof(WylderStunReturnRegression), nameof(Add)));
        try
        {
            foreach (PileType source in new[] { PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Hand })
            {
                Fixture(); var card = Card(source); Creature enemy = Enemy();
                CreatureCmd.Stun(enemy, _ => Task.CompletedTask, "NOTHING").GetAwaiter().GetResult();
                Check(enemy.IsStunned && card.Pile?.Type == PileType.Hand && card.EnergyCost.GetResolved() == 0,
                    "Successful native leaf Stun returns same instance and sets next-play0 from " + source);
                var history=typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderStunHistory")!;
                Check((int)AccessTools.Method(history,"Count").Invoke(null,new object[]{Owner})! == 1,"Successful native stun records combat history");
                Check(Returned.Count == (source == PileType.Hand ? 0 : 1), "Already-hand copy is discounted without duplicate move");
                card.EnergyCost.EndOfTurnCleanup();
                Check(card.EnergyCost.GetResolved() == 0, "Native next-play0 survives turn end");
                card.EnergyCost.AfterCardPlayedCleanup();
                Check(card.EnergyCost.GetResolved() == 3, "Native cleanup restores original3 after one play");
                CreatureCmd.Stun(enemy, _ => Task.CompletedTask, "NOTHING").GetAwaiter().GetResult();
                Check(card.EnergyCost.GetResolved() == 3, "An unchanged already-stunned state is not a new successful Stun");
                Check((int)AccessTools.Method(history,"Count").Invoke(null,new object[]{Owner})! == 1,"Already-stunned state does not add combat history");
            }
            Fixture();
            var a = Card(PileType.Draw); var b = Card(PileType.Discard); var playing = Card(PileType.Play); var deck = Card(PileType.Deck);
            Creature target = Enemy();
            CreatureCmd.Stun(target, _ => Task.CompletedTask, "NOTHING").GetAwaiter().GetResult();
            Check(Returned.Count == 2 && a.Pile?.Type == PileType.Hand && b.Pile?.Type == PileType.Hand, "Every existing combat copy returns, no new cards generated");
            Check(deck.Pile?.Type == PileType.Deck && deck.EnergyCost.GetResolved() == 3, "Permanent deck instance untouched");
            Check(playing.Pile?.Type == PileType.Play && playing.EnergyCost.GetResolved() == 3, "Resolving copy waits for play cleanup");
            playing.EnergyCost.AfterCardPlayedCleanup();
            Put(playing, PileType.Discard);
            playing.AfterCardChangedPilesLate(playing, PileType.Play, null!).GetAwaiter().GetResult();
            Check(playing.Pile?.Type == PileType.Hand && playing.EnergyCost.GetResolved() == 0, "Deferred copy returns after cleanup with a fresh next-play discount");
            playing.AfterCardChangedPilesLate(playing, PileType.Discard, null!).GetAwaiter().GetResult();
            Check(Returned.Count == 3, "Deferred move callback cannot recurse or duplicate");
            Fixture();
            var endTurn = Card(PileType.Discard);
            Type granted = typeof(Wylder).Assembly.GetType("NightMustStay.Core.Models.Cards.WylderGrantedDesperation")!;
            object batches = AccessTools.Field(granted, "Batches").GetValue(null)!;
            object batch = AccessTools.Method(batches.GetType(), "GetOrCreateValue")!.Invoke(batches, new object[] { Combat })!;
            var resolving = (ISet<Player>)AccessTools.Field(batch.GetType(), "Resolving").GetValue(batch)!;
            resolving.Add(Owner);
            endTurn.OnEnemyStunned().GetAwaiter().GetResult();
            Check(endTurn.Pile?.Type == PileType.Discard && Returned.Count == 0,
                "Desperation Stun defers return until native end-turn discard has finished");
            resolving.Remove(Owner);
            endTurn.AfterSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
            Check(endTurn.Pile?.Type == PileType.Hand && endTurn.EnergyCost.GetResolved() == 0 && Returned.Count == 1,
                "Deferred Desperation return enters hand after the discard node with next-play0");
            Fixture();
            var triumph = Triumph(PileType.Discard);
            batches = AccessTools.Field(granted, "Batches").GetValue(null)!;
            batch = AccessTools.Method(batches.GetType(), "GetOrCreateValue")!.Invoke(batches, new object[] { Combat })!;
            resolving = (ISet<Player>)AccessTools.Field(batch.GetType(), "Resolving").GetValue(batch)!;
            resolving.Add(Owner);
            triumph.OnEnemyStunned().GetAwaiter().GetResult();
            Check(triumph.Pile?.Type == PileType.Discard && Returned.Count == 0,
                "Desperation Stun defers Triumph until native end-turn discard has finished");
            resolving.Remove(Owner);
            triumph.AfterSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
            Check(triumph.Pile?.Type == PileType.Hand && Returned.Count == 1,
                "Deferred Triumph return enters hand after the discard node");
            Fixture();
            endTurn = Card(PileType.Discard);
            triumph = Triumph(PileType.Discard);
            batches = AccessTools.Field(granted, "Batches").GetValue(null)!;
            batch = AccessTools.Method(batches.GetType(), "GetOrCreateValue")!.Invoke(batches, new object[] { Combat })!;
            resolving = (ISet<Player>)AccessTools.Field(batch.GetType(), "Resolving").GetValue(batch)!;
            Player teammate = TrackingArrowRegression.MakePlayer();
            resolving.Add(teammate);
            endTurn.OnEnemyStunned().GetAwaiter().GetResult();
            triumph.OnEnemyStunned().GetAwaiter().GetResult();
            Check(Returned.Count == 0 && endTurn.Pile?.Type == PileType.Discard
                    && triumph.Pile?.Type == PileType.Discard,
                "A teammate's Desperation batch defers every player's Stun-return cards");
            resolving.Remove(teammate);
            endTurn.AfterSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
            triumph.AfterSideTurnEnd(null!, CombatSide.Player, new[] { Owner.Creature }).GetAwaiter().GetResult();
            Check(Returned.Count == 2 && endTurn.Pile?.Type == PileType.Hand
                    && triumph.Pile?.Type == PileType.Hand,
                "Multiplayer-deferred Stun returns enter hand only after Desperation resolution");
            Fixture(); var untouched = Card(PileType.Discard); var dead = Enemy(); dead.SetCurrentHpInternal(0);
            CreatureCmd.Stun(dead, _ => Task.CompletedTask, "NOTHING").GetAwaiter().GetResult();
            Check(untouched.Pile?.Type == PileType.Discard && untouched.EnergyCost.GetResolved() == 3 && Returned.Count == 0, "Dead enemy Stun does not trigger return");
            Fixture(); untouched = Card(PileType.Discard); Creature ally = Enemy(CombatSide.Player);
            CreatureCmd.Stun(ally, _ => Task.CompletedTask, "NOTHING").GetAwaiter().GetResult();
            Check(untouched.Pile?.Type == PileType.Discard && Returned.Count == 0, "Stunning a same-side creature does not trigger enemy return");
            Console.WriteLine($"PASS: {_checks} Critical Strike stun-return checks: successful native Stun, combat piles/same instances, next-play cost lifetime, play cleanup, post-discard Desperation return and invalid targets.");
        }
        finally { h.UnpatchAll(h.Id); }
    }
    private static void Fixture()
    { AccessTools.Method(Shared, "Fixture").Invoke(null, new object[] { false }); ((List<Creature>)AccessTools.Field(typeof(CombatState), "_enemies").GetValue(Combat)!).Clear(); Returned.Clear(); }
    private static Creature Enemy(CombatSide side = CombatSide.Enemy)
    {
        var model = ModelDb.Monster<BigDummy>().ToMutable();
        var enemy = new Creature(model, side, null) { CombatState = Combat }; model.SetUpForCombat();
        ((List<Creature>)AccessTools.Field(typeof(CombatState), side == CombatSide.Enemy ? "_enemies" : "_allies").GetValue(Combat)!).Add(enemy);
        return enemy;
    }
    private static WylderCriticalStrike Card(PileType pile)
    { var card = (WylderCriticalStrike)ModelDb.Card<WylderCriticalStrike>().ToMutable(); AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, Owner); Put(card, pile); return card; }
    private static WylderTriumph Triumph(PileType pile)
    { var card = (WylderTriumph)ModelDb.Card<WylderTriumph>().ToMutable(); AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, Owner); Put(card, pile); return card; }
    private static void Put(CardModel card, PileType pile)
    {
        if (pile == PileType.Deck) ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(Owner.Deck)!).Add(card);
        else AccessTools.Method(Shared, "Put").Invoke(null, new object[] { card, pile });
    }
    public static bool Add(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
    { Check(newPileType == PileType.Hand, "Return always uses native hand destination"); Returned.Add(card); Put(card, newPileType); __result = Task.FromResult(new CardPileAddResult { success = true, cardAdded = card }); return false; }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception("Stun return: " + reason); }
}
