using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using NightMustStay.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
internal static class WylderStableCardSettledPatch
{
    [HarmonyPostfix]
    private static void Settle(PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result) => __result = Finish(__result, choiceContext, cardPlay);
    private static async Task Finish(Task native, PlayerChoiceContext context, CardPlay play)
    { await native; WylderCombatEvents.RegisterOldPursuitConsumption(play); await WylderCombatEvents.CompleteCard(context, play); }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
internal static class WylderDamageEventsPatch
{
    [HarmonyPostfix]
    private static void Observe(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel cardSource, ref Task __result) =>
        __result = Finish(__result, choiceContext, target, result, props, dealer, cardSource);
    private static async Task Finish(Task native, PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel card)
    { await native; await WylderCombatEvents.AfterDamage(context, dealer, target, result, props, card); }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyEnergyCostInCombat))]
internal static class WylderPendingEnergyCostPatch
{
    [HarmonyPostfix]
    private static void Cost(CardModel card, ref decimal __result) => __result = WylderCombatEvents.ModifyCost(card, __result);
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
internal static class WylderTurnLedgerPatch
{
    [HarmonyPrefix]
    private static void Reset(CombatSide side, IReadOnlyList<Creature> participants)
    {
        if (side == CombatSide.Player)
            foreach (Creature creature in participants) if (creature.Player is { } player) WylderCombatEvents.StartTurn(player);
    }
}
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
internal static class WylderPendingStartRewardsPatch
{
    [HarmonyPostfix]
    private static void Pay(PlayerChoiceContext choiceContext, Player player, ref Task __result) => __result = Finish(__result, choiceContext, player);
    private static async Task Finish(Task native, PlayerChoiceContext context, Player player)
    { await native; await WylderCombatEvents.PayStartTurn(context, player); }
}
[HarmonyPatch]
internal static class WylderPendingTurnCleanupPatch
{
    private static System.Reflection.MethodBase TargetMethod() =>
        AccessTools.Method(typeof(Hook), "AfterTurnEnd") ?? AccessTools.Method(typeof(Hook), "AfterSideTurnEnd");
    [HarmonyPostfix]
    private static void Clean(CombatSide side, IEnumerable<Creature> participants, ref Task __result) => __result = Finish(__result, side, participants.ToArray());
    private static async Task Finish(Task native, CombatSide side, Creature[] participants)
    {
        await native;
        if (side == CombatSide.Player)
            foreach (Creature creature in participants) if (creature.Player is { } player) WylderCombatEvents.EndTurn(player);
    }
}
// The scope is the native Vigor.AfterAttack, not arbitrary negative Vigor
// changes. Thus direct losses from P062/P068 never produce consumption events.
[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.AfterAttack))]
internal static class WylderNativeVigorAttackScopePatch
{
    private sealed class Scope { internal int Depth; }
    private static readonly ConditionalWeakTable<VigorPower, Scope> Scopes = new();
    internal static bool Active(VigorPower power) => Scopes.TryGetValue(power, out var scope) && scope.Depth > 0;
    [HarmonyPrefix] private static void Before(VigorPower __instance) => Scopes.GetOrCreateValue(__instance).Depth++;
    [HarmonyPostfix] private static void After(VigorPower __instance, ref Task __result) => __result = Finish(__result, __instance);
    private static async Task Finish(Task native, VigorPower power)
    {
        try { await native; }
        finally
        {
            Scopes.GetOrCreateValue(power).Depth--;
            // Native Vigor assumes complete removal. Half-consumption keeps the
            // instance alive, so release the native attack binding for its next use.
            if (power.Amount > 0)
            {
                object data = AccessTools.Field(typeof(PowerModel), "_internalData").GetValue(power);
                AccessTools.Field(data.GetType(), "commandToModify").SetValue(data, null);
            }
        }
    }
}
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]
internal static class WylderNativeVigorConsumptionPatch
{
    [HarmonyPrefix]
    private static void Before(PowerModel power, ref decimal offset, out (Player Player, int Before) __state)
    {
        __state = (null, -1);
        if (power is VigorPower vigor && offset < 0 && WylderNativeVigorAttackScopePatch.Active(vigor))
        { __state = (power.Owner.Player, power.Amount); offset = WylderCombatEvents.AdjustAttackVigorConsumption(vigor, offset); }
    }
    [HarmonyPostfix]
    private static void After((Player Player, int Before) __state, ref Task<int> __result)
    { if (__state.Before >= 0) __result = Finish(__result, __state.Player, __state.Before); }
    private static async Task<int> Finish(Task<int> native, Player player, int before)
    {
        int after = await native;
        WylderCombatEvents.RegisterVigorConsumed(player, Math.Max(0, before - after));
        return after;
    }
}
