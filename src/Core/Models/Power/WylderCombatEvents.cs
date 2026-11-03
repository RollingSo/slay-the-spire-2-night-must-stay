using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Characters;

namespace NightMustStay.Core.Models.Power;

public enum WylderEffectSource { CardEffect, PowerEffect, RelicEffect }

// This ledger exists before any ability is deployed. Events spend their global
// first-event eligibility even when there is no matching power to reward.
public static class WylderCombatEvents
{
    internal sealed class State
    {
        internal object Combat;
        internal readonly HashSet<string> Seen = new();
        internal readonly List<CardPlay> Stack = new();
        internal int Breaks;
        internal int AttackDiscount, BreakDiscount, BreakDraw, SkillDiscount;
        internal bool HalfVigor, EnemyLifeLoss;
        internal int NextEnergy, NextBlock, NextVigor;
        internal WylderEffectSource Source = WylderEffectSource.CardEffect;
    }
    internal sealed class Resolution
    {
        internal readonly Queue<Func<PlayerChoiceContext, Task>> Rewards = new();
        internal bool Begun, Completed, Draining, OldPursuit, ConsumedPursuit, KeptPursuit, VigorConsumed, Broke, HalfVigor;
        internal int ThresholdReduction, BreakDraw;
        internal WylderEffectSource ParentSource;
    }
    private static readonly ConditionalWeakTable<Player, State> States = new();
    private static readonly ConditionalWeakTable<CardPlay, Resolution> Plays = new();
    private static bool Eligible(Player player) => player?.Character is Wylder;
    internal static State For(Player player)
    {
        State state = States.GetOrCreateValue(player);
        object combat = player.Creature.CombatState;
        if (!ReferenceEquals(state.Combat, combat))
        {
            state = new State { Combat = combat };
            States.Remove(player); States.Add(player, state);
        }
        return state;
    }
    private static bool PlayerTurn(Player player) => player.Creature.CombatState?.CurrentSide == CombatSide.Player;
    private static bool First(Player player, string name) => PlayerTurn(player) && For(player).Seen.Add(name);
    internal static CardPlay Current(Player player) => Eligible(player) ? For(player).Stack.LastOrDefault() : null;
    private static T Power<T>(Player player) where T : WylderAbilityPower => player.Creature.GetPower<T>();
    private static Task Block(PlayerChoiceContext context, Player player, int amount, CardPlay play) =>
        amount > 0 ? CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, play) : Task.CompletedTask;
    private static async Task Vigor(PlayerChoiceContext context, Player player, int amount, CardModel source)
    {
        if (amount > 0) await PowerCmd.Apply<VigorPower>(context, player.Creature, amount, player.Creature, source);
    }
    private static async Task Strength(PlayerChoiceContext context, Player player, int amount, CardModel source)
    {
        if (amount > 0) await PowerCmd.Apply<StrengthPower>(context, player.Creature, amount, player.Creature, source);
    }
    private static async Task InPowerScope(Player player, Func<Task> action)
    {
        State state = For(player); WylderEffectSource previous = state.Source;
        state.Source = WylderEffectSource.PowerEffect;
        try { await action(); } finally { state.Source = previous; }
    }
    private static void Queue(CardPlay play, Func<PlayerChoiceContext, Task> reward)
    {
        if (play != null) Plays.GetOrCreateValue(play).Rewards.Enqueue(reward);
    }
    private static async Task QueueOrRun(PlayerChoiceContext context, Player player, Func<PlayerChoiceContext, Task> reward)
    {
        CardPlay play = Current(player);
        if (play != null) Queue(play, reward);
        else await InPowerScope(player, () => reward(context));
    }

    public static bool ConsumedVigor(CardPlay play) => play != null && Plays.TryGetValue(play, out var state) && state.VigorConsumed;
    public static bool ConsumedOldPursuit(CardPlay play) => play != null && Plays.TryGetValue(play, out var state) && state.OldPursuit && !state.KeptPursuit;
    public static void PreserveHalfVigor(Player player) => For(player).HalfVigor = true;
    public static void SetNextHandAttackDiscount(Player player, int amount) => For(player).AttackDiscount = Math.Max(For(player).AttackDiscount, amount);
    public static void SetNextHandSkillDiscount(Player player, int amount) => For(player).SkillDiscount = Math.Max(For(player).SkillDiscount, amount);
    public static void SetNextHandBreakDiscount(Player player, int amount, int draw)
    {
        For(player).BreakDiscount = Math.Max(For(player).BreakDiscount, amount);
        For(player).BreakDraw = Math.Max(For(player).BreakDraw, draw);
    }
    public static void AddNextTurnVigor(Player player, int amount) => For(player).NextVigor += amount;

    // Called before the native BeforeCardPlayed listeners, after W0 was mapped.
    public static void BeginCard(CardPlay play)
    {
        Player player = play.Card.Owner;
        if (!Eligible(player)) return;
        Resolution data = Plays.GetOrCreateValue(play);
        if (data.Begun) return;
        data.Begun = true;
        State state = For(player);
        data.ParentSource = state.Source;
        state.Source = WylderEffectSource.CardEffect;
        state.Stack.Add(play);
        data.OldPursuit = WylderPursuitPower.IsActive(player.Creature);
        bool hand = WylderHandPlayContext.HandCount(play.Card, play).HasValue;
        if (play.Card.Type == CardType.Attack)
        {
            data.HalfVigor = state.HalfVigor; state.HalfVigor = false;
            if (First(player, "FirstAttack") && Power<WylderClashEmbersPower>(player) is { } heat)
            {
                int amount = heat.Sum(1, 2);
                Queue(play, c => Vigor(c, player, amount, play.Card));
            }
            if (play.Card is WylderCard attack && attack.HasBreak)
            {
                if (First(player, "FirstBreakAttack") && Power<WylderNightEdgePower>(player) is { } edge)
                    data.ThresholdReduction = edge.Highest(5, 7);
                if (hand)
                {
                    data.BreakDraw = state.BreakDraw;
                    state.BreakDiscount = state.BreakDraw = 0;
                }
            }
            if (hand) state.AttackDiscount = 0;
        }
        if (hand && play.Card.Type == CardType.Skill) state.SkillDiscount = 0;
    }
    public static decimal EffectiveBreak(WylderCard card, CardPlay play, decimal threshold)
    {
        if (threshold <= 0 || !card.IsMutable || !Eligible(card.Owner)) return threshold;
        int reduction = play != null && Plays.TryGetValue(play, out var p) ? p.ThresholdReduction :
            !For(card.Owner).Seen.Contains("FirstBreakAttack") ? Power<WylderNightEdgePower>(card.Owner)?.Highest(5, 7) ?? 0 : 0;
        return Math.Max(1m, threshold - reduction);
    }
    public static decimal ModifyCost(CardModel card, decimal cost)
    {
        if (!card.IsMutable || !Eligible(card.Owner) || card.Pile?.Type != PileType.Hand || card.EnergyCost.CostsX) return cost;
        State state = For(card.Owner);
        int reduction = card.Type == CardType.Attack ? state.AttackDiscount : card.Type == CardType.Skill ? state.SkillDiscount : 0;
        if (card.Type == CardType.Attack && card is WylderCard { HasBreak: true }) reduction += state.BreakDiscount;
        return Math.Max(0, cost - reduction);
    }

    public static void RegisterBreak(CardPlay play)
    {
        if (play == null || !Eligible(play.Card.Owner)) return;
        Resolution data = Plays.GetOrCreateValue(play);
        if (data.Broke) return;
        data.Broke = true;
        Player player = play.Card.Owner;
        if (data.BreakDraw > 0)
        {
            int draw = data.BreakDraw;
            Queue(play, c => CardPileCmd.Draw(c, draw, player));
        }
        if (Power<WylderEruptionPower>(player) is { } eruption)
        {
            int damage = eruption.Sum(WylderEruption.BaseDamage, WylderEruption.UpgradedDamage);
            Queue(play, async c => {
                var enemies = player.Creature.CombatState.GetOpponentsOf(player.Creature).Where(e => e.IsAlive).ToArray();
                if (enemies.Length > 0) await CreatureCmd.Damage(c, enemies, damage, ValueProp.Unpowered, player.Creature);
            });
        }
        if (!PlayerTurn(player)) return;
        int count = ++For(player).Breaks;
        if (count == 1)
        {
            if (Power<WylderOpeningHuntPower>(player) is { } hunt)
            {
                int draw = hunt.Copies, vigor = hunt.Sum(0, 2);
                Queue(play, async c => { await CardPileCmd.Draw(c, draw, player); await Vigor(c, player, vigor, play.Card); });
            }
            if (Power<WylderBattlefieldMightPower>(player) is { } might)
            { int amount = might.Sum(1, 2); Queue(play, c => Strength(c, player, amount, play.Card)); }
            if (Power<WylderNightEdgePower>(player) is { } edge)
            { int amount = edge.Highest(3, 4); Queue(play, c => Vigor(c, player, amount, play.Card)); }
        }
        if (Power<WylderVeteranEdgePower>(player) is { } veteran)
        {
            int amount = veteran.Copies;
            Queue(play, c => Strength(c, player, amount, play.Card));
        }
    }
    public static async Task RegisterEntry(PlayerChoiceContext context, Creature owner, CardModel card, WylderEffectSource source)
    {
        Player player = owner.Player;
        if (!Eligible(player)) return;
        if (Power<WylderRemainingEmberPower>(player) is { } ember)
        {
            int basic = ember.BasicCopies, total = ember.Copies;
            await QueueOrRun(context, player, async _ => {
                for (int i = 0; i < total; i++)
                {
                    var coating = owner.CombatState.CreateCard<WylderFireCoating>(player);
                    if (i >= basic) CardCmd.Upgrade(coating);
                    await CardPileCmd.AddGeneratedCardToCombat(coating, PileType.Hand, player);
                }
            });
        }
        if (!PlayerTurn(player)) return;
        if (Power<WylderWindwardStancePower>(player) is { } wind && First(player, "WindwardDeployedEntry"))
        {
            int amount = wind.Sum(4, 6); CardPlay play = Current(player);
            await QueueOrRun(context, player, c => Block(c, player, amount, play));
        }
        if (source == WylderEffectSource.CardEffect && First(player, "CardPursuitEntry") && Power<WylderPursuitCommandPower>(player) is { } command)
        {
            int draw = Math.Min(2, command.Sum(1, 2));
            await QueueOrRun(context, player, c => CardPileCmd.Draw(c, draw, player));
        }
    }
    public static void RegisterKeptPursuit(CardPlay play)
    {
        if (play != null) Plays.GetOrCreateValue(play).KeptPursuit = true;
    }
    public static void QueuePursuitExit(CardPlay play, CardModel[] cards)
    {
        Queue(play, c => WylderFollowThroughCard.PlaySnapshot(c, cards));
    }
    public static void RegisterOldPursuitConsumption(CardPlay play)
    {
        if (play == null || !Eligible(play.Card.Owner)) return;
        Resolution data = Plays.GetOrCreateValue(play);
        if (!data.OldPursuit || data.ConsumedPursuit || data.KeptPursuit) return;
        data.ConsumedPursuit = true;
        Player player = play.Card.Owner;
        if (!First(player, "OldPursuitConsumption") || Power<WylderPursuitStancePower>(player) is not { } stance) return;
        if (play.Card.Type == CardType.Attack) Queue(play, _ => PlayerCmd.GainEnergy(1, player));
        else if (play.Card.Type == CardType.Skill)
        { int block = Math.Min(20, stance.Sum(8, 12)); Queue(play, c => Block(c, player, block, play)); }
    }
    public static Task RegisterSkillStun(PlayerChoiceContext context, CardPlay play)
    {
        if (play?.Card.Type != CardType.Skill || !Eligible(play.Card.Owner)) return Task.CompletedTask;
        Player player = play.Card.Owner;
        if (First(player, "SkillStun") && Power<WylderGuardResurgencePower>(player) is { } power)
        {
            For(player).NextEnergy += power.Copies;
            For(player).NextBlock += power.Sum(4, 7);
        }
        return Task.CompletedTask;
    }
    public static async Task VigorGained(PlayerChoiceContext context, PowerModel power, decimal amount, CardModel cardSource)
    {
        Player player = power.Owner?.Player;
        if (power is not VigorPower || amount <= 0 || !Eligible(player) || !PlayerTurn(player)) return;
        if (First(player, "VigorGained") && Power<WylderBloodStillHotPower>(player) is { } blood)
        { int block = blood.Sum(5, 7); CardPlay play = Current(player); await QueueOrRun(context, player, c => Block(c, player, block, play)); }
        if (For(player).Source != WylderEffectSource.CardEffect || cardSource == null || Current(player)?.Card != cardSource) return;
        if (First(player, "CardVigorGained") && Power<WylderFieryBloodPower>(player) is { } fiery)
        { int vigor = fiery.Sum(2, 3); await QueueOrRun(context, player, c => Vigor(c, player, vigor, cardSource)); }
    }
    // Only called from the native Vigor.AfterAttack mutation, never from direct
    // skill losses. Reduction happens before consumption, not via a refund.
    public static decimal AdjustAttackVigorConsumption(VigorPower power, decimal offset)
    {
        Player player = power.Owner?.Player; CardPlay play = Current(player);
        if (offset >= 0 || !Eligible(player) || play == null) return offset;
        if (Plays.GetOrCreateValue(play).HalfVigor) return -Math.Ceiling(-offset / 2m);
        return offset;
    }
    public static void RegisterVigorConsumed(Player player, int amount)
    {
        CardPlay play = Current(player);
        if (amount <= 0 || play?.Card.Type != CardType.Attack || !Eligible(player)) return;
        Plays.GetOrCreateValue(play).VigorConsumed = true;
        if (!First(player, "VigorConsumed")) return;
        if (Power<WylderEmberReservePower>(player) is { } reserve)
        { int block = reserve.Sum(4, 6); Queue(play, c => Block(c, player, block, play)); }
        if (Power<WylderFlameGuardStancePower>(player) is { } guard)
        {
            int block = guard.Sum(0, 3);
            Queue(play, async c => { await WylderPursuitPower.Enter(c, player.Creature, play.Card, WylderEffectSource.PowerEffect); await Block(c, player, block, play); });
        }
    }
    public static async Task AfterDamage(PlayerChoiceContext context, Creature dealer, Creature target, DamageResult result, ValueProp props, CardModel card)
    {
        if (props.IsPoweredAttack() && card?.Type == CardType.Attack && dealer?.Player is { } boldPlayer
            && Eligible(boldPlayer) && target.Side != dealer.Side && result.TotalDamage > 20
            && Power<WylderBoldnessPower>(boldPlayer) is { } bold)
            await InPowerScope(boldPlayer, () => Block(context, boldPlayer, bold.Sum(5,7), Current(boldPlayer)));
        if (props.IsPoweredAttack() && card?.Type == CardType.Attack && dealer?.Player is { } attacker && Eligible(attacker)
            && target.Side != dealer.Side && target.IsAlive && result.TotalDamage > 20 && Power<WylderRuinousGhostflamePower>(attacker) is { } ghost)
            await InPowerScope(attacker, async () => { await PowerCmd.Apply<FreezePower>(context, target, ghost.Sum(2, 3), dealer, card); });
        if (target?.Player is not { } player || !Eligible(player) || dealer == null || dealer.Side == target.Side
            || !props.IsPoweredAttack() || result.UnblockedDamage <= 0) return;
        State state = For(player);
        if (state.EnemyLifeLoss) return;
        state.EnemyLifeLoss = true;
        if (Power<WylderUnyieldingSpiritPower>(player) is not { } spirit || !target.IsAlive) return;
        int block = spirit.Sum(8, 12);
        await InPowerScope(player, async () => { await Block(context, player, block, null); await WylderPursuitPower.Enter(context, target, null, WylderEffectSource.PowerEffect); });
    }
    public static async Task CompleteCard(PlayerChoiceContext context, CardPlay play)
    {
        Player player = play.Card.Owner;
        if (!Eligible(player)) return;
        Resolution data = Plays.GetOrCreateValue(play);
        if (data.Completed || data.Draining) return;
        data.Draining = true;
        try
        {
            // Hook.AfterCardPlayed has awaited every AfterCardPlayedLate
            // listener, including the old Pursuit draw. Derived ability rewards
            // append to this same queue; drain to a fixed point before hand tests.
            while (data.Rewards.Count > 0)
            { var reward = data.Rewards.Dequeue(); await InPowerScope(player, () => reward(context)); }
            if (WylderHandPlayContext.HandCount(play.Card, play).HasValue && PlayerTurn(player))
            {
                var hand = PileType.Hand.GetPile(player).Cards;
                if (play.Card.Type == CardType.Attack && hand.Count == 1 && hand[0].Type == CardType.Skill
                    && Power<WylderBattleInstinctPower>(player) is { } instinct && First(player, "InstinctDeployedHand"))
                { hand[0].EnergyCost.AddThisTurn(-instinct.Sum(1, 2)); hand[0].InvokeEnergyCostChanged(); }
                if (play.Card is not WylderSwiftBattleWill && hand.Count <= 2 && First(player, "StableLowHand")
                    && Power<WylderSwiftBattleWillPower>(player) is { } swift)
                {
                    int energy = swift.Copies, block = swift.Sum(4, 6);
                    await InPowerScope(player, async () => { await PlayerCmd.GainEnergy(energy, player); await Block(context, player, block, play); });
                }
            }
            // Stable rewards currently cannot draw; still settle their derived
            // effects before releasing the outer resolution frame.
            while (data.Rewards.Count > 0)
            { var reward = data.Rewards.Dequeue(); await InPowerScope(player, () => reward(context)); }
            data.Completed = true;
        }
        finally
        {
            data.Draining = false;
            For(player).Stack.Remove(play);
            For(player).Source = data.ParentSource;
        }
    }
    public static void StartTurn(Player player)
    {
        if (!Eligible(player)) return;
        State state = For(player); state.Seen.Clear(); state.Breaks = 0; state.EnemyLifeLoss = false;
    }
    public static async Task PayStartTurn(PlayerChoiceContext context, Player player)
    {
        if (!Eligible(player)) return;
        State state = For(player);
        int energy = state.NextEnergy, block = state.NextBlock, vigor = state.NextVigor;
        state.NextEnergy = state.NextBlock = state.NextVigor = 0;
        await InPowerScope(player, async () =>
        {
            if (energy > 0) await PlayerCmd.GainEnergy(energy, player);
            await Block(context, player, block, null);
            await Vigor(context, player, vigor, null);
        });
    }
    public static void EndTurn(Player player)
    {
        if (!Eligible(player)) return;
        State state = For(player);
        state.AttackDiscount = state.BreakDiscount = state.BreakDraw = state.SkillDiscount = 0;
        state.HalfVigor = false;
    }
}
