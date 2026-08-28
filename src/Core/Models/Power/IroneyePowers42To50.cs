using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

public sealed class ApproachingVenomFangPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (power is not DistancePower || power.Owner != Owner || amount == 0m)
            return;

        decimal hiddenPoison = decimal.Abs(amount) * Amount;
        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        if (enemies.Length == 0)
            return;

        Flash();
        foreach (Creature enemy in enemies)
        {
            await PowerCmd.Apply<HiddenPoisonPower>(
                choiceContext,
                enemy,
                hiddenPoison,
                Owner,
                cardSource);
        }
    }

}

public sealed class AllThingsWitherPower : PowerModel
{
    private sealed class Data
    {
        public readonly HashSet<Creature> TriggeredTargets = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<PoisonBurstPower>(),
            HoverTipFactory.FromPower<HiddenPoisonPower>(),
        };

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (dealer != Owner
            || cardSource == null
            || cardSource.Type != CardType.Attack
            || !props.HasFlag(ValueProp.Move)
            || result.TotalDamage <= 0m
            || !target.IsAlive)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (!data.TriggeredTargets.Add(target))
            return;

        Flash();
        for (int i = 0; i < decimal.ToInt32(Amount) && target.IsAlive; i++)
            await IroneyeHiddenPoison.BurstOnce(choiceContext, target, Owner, cardSource);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner)
            GetInternalData<Data>().TriggeredTargets.Clear();
        return Task.CompletedTask;
    }
}

public sealed class HeavenlyEyeFormPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Approach>(true)
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>(true));

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || Owner.Player == null || Amount <= 0m)
            return;

        Flash();
        for (int i = 0; i < (int)Amount; i++)
        {
            CardModel generated = Owner.Player.RunState.Rng.Niche.NextItem(
                new CardModel[]
                {
                    combatState.CreateCard<Approach>(Owner.Player),
                    combatState.CreateCard<Retreat>(Owner.Player),
                });
            CardCmd.Upgrade(generated);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    generated,
                    PileType.Hand,
                    Owner.Player));
        }
    }
}

public sealed class SharedIntelligencePower : PowerModel, IMarkTriggerPower
{
    private sealed class Data
    {
        public int TriggersThisTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<MarkPower>() };

    public async Task AfterMarkTriggered(
        PlayerChoiceContext choiceContext,
        Creature markedTarget,
        CardModel triggeringCard)
    {
        Data data = GetInternalData<Data>();
        if (Owner.Player == null || data.TriggersThisTurn >= Amount)
            return;

        data.TriggersThisTurn++;
        Flash();
        foreach (var teammate in CombatState.Players.Where(player =>
                     player.Creature != Owner && player.Creature.IsAlive))
        {
            await CardPileCmd.Draw(choiceContext, 1, teammate);
        }
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side == Owner.Side && creatures.Contains(Owner))
            GetInternalData<Data>().TriggersThisTurn = 0;

        return Task.CompletedTask;
    }
}

public sealed class IronEyePower : PowerModel
{
    private sealed class Data
    {
        public decimal AppliedDexterity;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<DexterityPower>(),
        };

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        await RefreshTeammateDexterity(
            new BlockingPlayerChoiceContext(),
            cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if ((power is DistancePower && power.Owner == Owner) || power == this)
            await RefreshTeammateDexterity(choiceContext, cardSource);
    }

    private async Task RefreshTeammateDexterity(
        PlayerChoiceContext choiceContext,
        CardModel cardSource)
    {
        decimal distance = Owner.GetPower<DistancePower>()?.Amount ?? 0m;
        decimal desired = decimal.Max(0m, distance) * Amount;
        Data data = GetInternalData<Data>();
        decimal delta = desired - data.AppliedDexterity;
        if (delta == 0m || CombatState == null)
            return;

        data.AppliedDexterity = desired;
        Flash();
        foreach (var teammate in CombatState.Players.Where(player =>
                     player.Creature != Owner && player.Creature.IsAlive))
        {
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                teammate.Creature,
                delta,
                Owner,
                cardSource);
        }
    }
}

public sealed class ObservationPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
