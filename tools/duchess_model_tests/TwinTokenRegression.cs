using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;

internal static class TwinTokenRegression
{
    private record Snapshot(int Energy, int HostSixthSense, int ClientSixthSense);

    public static void Run()
    {
        var harmony = new HarmonyLib.Harmony("NightMustStay.TwinToken.Regression");
        var previousLocalId = LocalContext.NetId;
        try
        {
            foreach (string name in new[] { "AdvanceAndRetreatTargetValidationPatch", "TwinTokenCanPlayPatch" })
            {
                var patch = typeof(WylderTwinToken).Assembly.GetType("NightMustStay.Core.Patches." + name)!;
                harmony.CreateClassProcessor(patch).Patch();
            }
            var inject = AccessTools.Method(typeof(ModelDb), "Inject");
            foreach (var type in typeof(ModelDb).Assembly.GetTypes().Where(type => !type.IsAbstract &&
                (typeof(ActModel).IsAssignableFrom(type) || typeof(EncounterModel).IsAssignableFrom(type)
                    || typeof(BadgeModel).IsAssignableFrom(type) || type.Name == "MultiplayerScalingModel")))
                if (!ModelDb.Contains(type))
                    inject.Invoke(null, new object[] { type });

            // The console fixture has no Godot battle scene. Keep native action validation and
            // resource spending, then execute the real card and pile commands without the UI wrapper.
            harmony.Patch(AccessTools.Method(typeof(CardModel), nameof(CardModel.OnPlayWrapper)),
                prefix: new HarmonyMethod(typeof(TwinTokenRegression), nameof(PlayWithoutScene)));
            var executeAction = AccessTools.Method(typeof(PlayCardAction), "ExecuteAction");
            harmony.Patch(AccessTools.AsyncMoveNext(executeAction),
                transpiler: new HarmonyMethod(typeof(TwinTokenRegression), nameof(ReplaceSceneLogging)));
            foreach (bool upgraded in new[] { false, true })
            foreach (bool targetSelf in new[] { false, true })
            {
                Snapshot host = PlayAs(localId: 1, upgraded, targetSelf, soleSurvivor: false);
                Snapshot client = PlayAs(localId: 2, upgraded, targetSelf, soleSurvivor: false);
                Check(host == client, "Host and client reach the same state after the client's play.");
                if (targetSelf)
                    Check(host == PlayAs(localId: 2, upgraded, targetSelf, soleSurvivor: true),
                        "The surviving card owner can still select themselves.");
            }
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            NetCombatCardDb.Instance.ClearCardsForTesting();
            LocalContext.NetId = previousLocalId;
        }
        Console.WriteLine("PASS: Twin Token native play action, matching host/client piles and energy, self/teammate targeting, sole survivor, and 1/0 costs.");
    }

    private static Snapshot PlayAs(ulong localId, bool upgraded, bool targetSelf, bool soleSurvivor)
    {
        LocalContext.NetId = localId;
        var host = MakePlayer(1);
        var client = MakePlayer(2);
        var run = RunState.CreateForTest(new[] { host, client }, Array.Empty<ActModel>(), seed: "TWIN");
        var combat = new CombatState(runState: run);
        foreach (var player in new[] { host, client })
        {
            player.ResetCombatState();
            combat.AddPlayer(player);
            player.PlayerCombatState!.Energy = 3;
        }
        var permanent = run.CreateCard<WylderTwinToken>(client);
        client.Deck.AddInternal(permanent, silent: true);
        if (upgraded)
            permanent.UpgradeInternal();
        var token = (WylderTwinToken)combat.CloneCard(permanent);
        token.DeckVersion = permanent;
        client.PlayerCombatState!.Hand.AddInternal(token, silent: true);
        var target = targetSelf ? client.Creature : host.Creature;
        if (soleSurvivor)
            host.Creature.SetCurrentHpInternal(0);

        var nonPlayer = new Creature(host, 10, 10);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(nonPlayer, null);
        Check(!token.IsValidTarget(null) && !token.IsValidTarget(nonPlayer), "Reject missing and non-player targets.");
        if (soleSurvivor)
        {
            Check(!token.IsValidTarget(host.Creature), "Reject a dead teammate.");
            client.PlayerCombatState.Energy = 0;
            Check(token.CanPlay() == upgraded, "The lone-survivor exception still enforces energy cost.");
            client.PlayerCombatState.Energy = 3;
        }
        Check(token.CanPlayTargeting(target), "The native pre-play checks accept the selected player.");

        const uint networkCardId = 25;
        var cards = (Dictionary<uint, CardModel>)AccessTools.Field(typeof(NetCombatCardDb), "_idToCard")
            .GetValue(NetCombatCardDb.Instance)!;
        cards[networkCardId] = token;
        var action = new PlayCardAction(client, NetCombatCard.ForTesting(networkCardId), token.Id, target.CombatId);
        ((Task)AccessTools.Method(typeof(PlayCardAction), "ExecuteAction").Invoke(action, null)!).GetAwaiter().GetResult();

        Check(!client.Deck.Cards.Contains(permanent), "Remove the source card from the original owner's deck.");
        Check(token.Pile == null && token.HasBeenRemovedFromState, "Remove the played copy from combat.");
        var recipient = target.Player!;
        var generated = recipient.Deck.Cards.OfType<WylderSixthSense>().Single();
        Check(!generated.IsUpgraded, "Both versions add one unupgraded Sixth Sense to the chosen deck.");
        Check(client.PlayerCombatState.Energy == (upgraded ? 3 : 2) && host.PlayerCombatState!.Energy == 3,
            "Only the card owner pays the original or upgraded cost.");
        return new Snapshot(client.PlayerCombatState.Energy,
            host.Deck.Cards.OfType<WylderSixthSense>().Count(),
            client.Deck.Cards.OfType<WylderSixthSense>().Count());
    }

    private static Player MakePlayer(ulong netId)
    {
        var player = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
        AccessTools.Field(typeof(Player), "<NetId>k__BackingField").SetValue(player, netId);
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(player, ModelDb.Character<Wylder>());
        AccessTools.Field(typeof(Player), "<UnlockState>k__BackingField").SetValue(player, UnlockState.none);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, new Creature(player, 70, 70));
        return player;
    }

    private static IEnumerable<CodeInstruction> ReplaceSceneLogging(IEnumerable<CodeInstruction> instructions) =>
        instructions.MethodReplacer(AccessTools.Method(typeof(Log), nameof(Log.Info)),
            AccessTools.Method(typeof(TwinTokenRegression), nameof(SkipLog)));

    private static void SkipLog(string message, int skipFrames) { }

    private static bool PlayWithoutScene(CardModel __instance, PlayerChoiceContext choiceContext, Creature target, ref Task __result)
    {
        if (__instance is not WylderTwinToken token)
            return true;
        token.RemoveFromCurrentPile();
        token.Owner.PlayerCombatState!.PlayPile.AddInternal(token, silent: true);
        var play = TrackingArrowRegression.MakePlay(token, token.Owner);
        AccessTools.Property(typeof(CardPlay), nameof(CardPlay.Target)).SetValue(play, target);
        __result = (Task)AccessTools.Method(typeof(WylderTwinToken), "OnPlay").Invoke(token, new object[] { choiceContext, play })!;
        return false;
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }
}
