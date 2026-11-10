using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

// Definition-only model for the icon-bearing card glossary; never applied.
public sealed class WylderBreakDescriptionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

public sealed class WylderPursuitPower : PowerModel
{
    private sealed class Data { public readonly Dictionary<CardPlay, bool> Plays = new(); public readonly HashSet<CardPlay> Kept = new(); }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public static bool IsActive(Creature owner) => owner?.HasPower<WylderPursuitPower>() == true;
    public static async Task Enter(PlayerChoiceContext context, Creature owner, CardModel source,
        WylderEffectSource sourceKind = WylderEffectSource.CardEffect)
    {
        // A fresh entry during the consuming card must survive that card.
        if (owner.GetPower<WylderPursuitPower>() is { } existing)
        {
            foreach (CardPlay play in existing.GetInternalData<Data>().Plays.Keys.ToArray())
                existing.GetInternalData<Data>().Plays[play] = true;
        }
        else await PowerCmd.Apply<WylderPursuitPower>(context, owner, 1m, owner, source);
        if (IsActive(owner)) await WylderCombatEvents.RegisterEntry(context, owner, source, sourceKind);
    }
    private int EffectMultiplier => Owner.HasPower<WylderNightFormPower>() ? 2 : 1;
    internal decimal AttackMultiplier(Creature dealer, ValueProp props, CardModel source) =>
        dealer == Owner && source?.Type == CardType.Attack && props.IsPoweredAttack() ? 1m + 0.5m * EffectMultiplier : 1m;
    internal void KeepFor(CardPlay play)
    {
        var data = GetInternalData<Data>();
        if (data.Plays.ContainsKey(play)) { data.Kept.Add(play); WylderCombatEvents.RegisterKeptPursuit(play); }
    }
    // The engine chooses the total before its native CardPlay/Replay loop.
    // Each replay then deals damage and generates Fire Coating normally.
    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount) =>
        card is WylderFlamingStrike && card.Owner?.Creature == Owner
            ? playCount + card.DynamicVars.Repeat.IntValue : playCount;
    internal static bool ConsumesOnPlay(CardType type) =>
        type is CardType.Attack or CardType.Skill;
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner.Creature == Owner && ConsumesOnPlay(play.Card.Type))
        {
            Data data = GetInternalData<Data>();
            data.Plays.TryAdd(play, false);
        }
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner.Creature != Owner || !GetInternalData<Data>().Plays.Remove(play, out bool reentered)) return;
        if (GetInternalData<Data>().Kept.Remove(play)) return;
        int skillReward = play.Card.Type == CardType.Skill ? EffectMultiplier : 0;
        WylderCombatEvents.RegisterOldPursuitConsumption(play);
        if (!reentered)
        {
            var followThrough = WylderFollowThroughCard.Snapshot(Owner.Player);
            await PowerCmd.Remove(this);
            WylderCombatEvents.QueuePursuitExit(play, followThrough);
        }
        if (skillReward > 0)
        {
            await PlayerCmd.GainEnergy(skillReward, Owner.Player);
            await CardPileCmd.Draw(context, skillReward, Owner.Player);
        }
    }
    // Deliberately no turn-end hook: pursuit survives ending a turn.
}

public sealed class WylderUppercutStrengthDownPower : TemporaryStrengthPower
{
    // Native temporary enemy Strength loss, matching the project's existing
    // FrightenedBird/AirRendingArrow. The enemy's upcoming turn is affected.
    public override AbstractModel OriginModel => ModelDb.Card<WylderStampUpwardCut>();
    protected override bool IsPositive => false;
}
