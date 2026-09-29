using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

public sealed class DuchessZeroCostAttackPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props,
        Creature dealer, CardModel cardSource, CardPlay cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() && cardSource?.Type == CardType.Attack
        && cardSource.EnergyCost.GetResolved() == 0 ? Amount : 0m;
}

public sealed class DuchessGracefulSwordDancePower : PowerModel
{
    private sealed class Data { public bool PreviousWasDodge; }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) GetInternalData<Data>().PreviousWasDodge = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner.Creature != Owner) return;
        Data data = GetInternalData<Data>();
        bool dodge = play.Card is DuchessDodge;
        bool trigger = dodge && data.PreviousWasDodge;
        data.PreviousWasDodge = dodge;
        if (!trigger || !CombatState.HittableEnemies.Any(enemy => enemy.IsAlive)) return;
        Flash();
        await DamageCmd.Attack(Amount).TargetingAllOpponents(CombatState).Execute(context);
    }
}

public sealed class DuchessPhantomKillerPower : PowerModel
{
    private sealed class Data { public bool Triggered; }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterDamageGiven(PlayerChoiceContext context, Creature dealer, DamageResult result,
        ValueProp props, Creature target, CardModel cardSource)
    {
        if (GetInternalData<Data>().Triggered || dealer != Owner || !result.WasTargetKilled
            || !Owner.HasPower<DuchessConcealmentPower>() || target.Side == Owner.Side)
            return Task.CompletedTask;
        DuchessPhantomKiller deckCard = PileType.Deck.GetPile(Owner.Player).Cards
            .OfType<DuchessPhantomKiller>().FirstOrDefault();
        if (deckCard == null) return Task.CompletedTask;
        GetInternalData<Data>().Triggered = true;
        deckCard.PendingNextCombatStrength = Math.Max(deckCard.PendingNextCombatStrength,
            decimal.ToInt32(Amount));
        Flash();
        return Task.CompletedTask;
    }
}
