using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

public sealed class DuchessConcealmentPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockMultiplicative(
        Creature target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay) =>
        target == Owner && cardSource != null ? 1.25m : 1m;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner?.Creature == Owner && play.Card.Type == CardType.Attack)
            await PowerCmd.Decrement(this);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}

public sealed class DuchessReactionDrawBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card.Owner?.Creature != Owner || oldPileType != PileType.Draw
            || card.Pile?.Type != PileType.Hand || card is not DuchessCard { HasReaction: true }
            || Owner.Player.PlayerCombatState.Phase != PlayerTurnPhase.Play)
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

public sealed class DuchessRadiantBladeGrowthPower : PowerModel
{
    [SavedProperty]
    public decimal TotalGrowth { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card is not DuchessRadiantBlade || play.Card.Owner?.Creature != Owner)
            return Task.CompletedTask;
        TotalGrowth += Amount;
        foreach (DuchessRadiantBlade blade in Owner.Player.PlayerCombatState.AllCards.OfType<DuchessRadiantBlade>())
        {
            blade.DynamicVars.Damage.BaseValue += Amount;
        }
        Flash();
        return Task.CompletedTask;
    }
}

public sealed class DuchessFullBlockRadiantBladePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature dealer, CardModel card)
    {
        if (target != Owner || dealer?.Side == Owner.Side || !props.IsPoweredAttack()
            || result.UnblockedDamage > 0) return;
        Flash();
        for (int i = 0; i < Amount; i++)
        {
            DuchessRadiantBlade blade = Owner.Player.PlayerCombatState.AllCards.First().CombatState
                .CreateCard<DuchessRadiantBlade>(Owner.Player);
            if (Owner.HasPower<DuchessRadiantBladeGrowthPower>())
                blade.DynamicVars.Damage.BaseValue += Owner.GetPower<DuchessRadiantBladeGrowthPower>().TotalGrowth;
            CardPileAddResult added = await CardPileCmd.AddGeneratedCardToCombat(
                blade, PileType.Hand, Owner.Player, CardPilePosition.Top);
            CardCmd.PreviewCardPileAdd(added);
        }
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player == Owner.Player) await PowerCmd.Remove(this);
    }
}

public sealed class DuchessEndTurnDodgePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    [SavedProperty]
    public decimal UpgradedDodgeCount { get; set; }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        for (int i = 0; i < Amount; i++)
        {
            DuchessDodge dodge = Owner.Player.PlayerCombatState.AllCards.First().CombatState
                .CreateCard<DuchessDodge>(Owner.Player);
            if (i < UpgradedDodgeCount) CardCmd.Upgrade(dodge);
            CardPileAddResult added = await CardPileCmd.AddGeneratedCardToCombat(
                dodge, PileType.Draw, Owner.Player, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(added);
        }
    }
}

public sealed class DuchessEndTurnRetainPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeFlush(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        CardPile hand = PileType.Hand.GetPile(player);
        int maximum = Math.Min(decimal.ToInt32(Amount), hand.Cards.Count);
        if (maximum > 0)
        {
            CardModel[] retained = (await CardSelectCmd.FromCombatPile(
                context, hand, player,
                new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_TO_RETAIN"), 0, maximum)))
                .ToArray();
            foreach (CardModel card in retained) card.GiveSingleTurnRetain();
        }
        await PowerCmd.Remove(this);
    }
}

public sealed class DuchessBeatPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    [SavedProperty]
    public bool TriggeredThisTurn { get; set; }

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) TriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public async Task OnMomentFive(PlayerChoiceContext context)
    {
        if (TriggeredThisTurn) return;
        TriggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}

public sealed class DuchessMomentFiveBlockPower : DuchessMomentFiveRewardPower
{
    protected override Task ResolveMomentFive(PlayerChoiceContext context) =>
        CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
}

public sealed class DuchessEternalRestagePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public Task OnMomentTwelve(PlayerChoiceContext context)
    {
        PlayerCmd.EndTurn(Owner.Player, false, null);
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        CardModel source = Owner.Player.PlayerCombatState.AllCards.First();
        decimal damage = decimal.Floor(DuchessMomentPower.DamageDealtThisTurn(source) / 3m)
            * (Amount / 3m);
        if (damage <= 0) return;
        Flash();
        await DamageCmd.Attack(damage).CompatFromCard(source)
            .TargetingAllOpponents(source.CombatState).Execute(context);
    }
}

public sealed class DuchessReplayMomentThreePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount) =>
        card.Owner?.Creature == Owner && DuchessMomentPower.Current(Owner.Player) == 3
            ? playCount + decimal.ToInt32(Amount) : playCount;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}
