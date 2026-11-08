using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Relics;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Power;

public sealed class RevenantSummonControllerPower : PowerModel
{
    private sealed record PendingRoutedDamage(
        decimal Amount,
        Creature Dealer,
        CardModel CardSource);

    private PendingRoutedDamage _pendingRoutedDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // The controller is an implementation detail; its hooks remain active, but
    // it should never occupy a visible power slot in the combat UI.
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner || amount <= 0m || !props.IsPoweredAttack())
            return amount;

        RevenantSummonManager manager = RevenantSummonManager.For(Owner.Player);
        if (!GetDamageGuards(manager).Any())
            return amount;

        // Capture the actual HP loss after the Revenant's block and regular
        // HP-loss modifiers have resolved. The asynchronous follow-up then
        // routes that single damage instance through one ordered chain.
        _pendingRoutedDamage = new PendingRoutedDamage(amount, dealer, cardSource);
        return 0m;
    }

    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        PendingRoutedDamage pending = _pendingRoutedDamage;
        _pendingRoutedDamage = null;
        if (pending == null || pending.Amount <= 0m)
            return;

        RevenantSummonManager manager = RevenantSummonManager.For(Owner.Player);
        decimal remaining = pending.Amount;
        ValueProp routedProps = ValueProp.Unblockable | ValueProp.Unpowered;

        foreach (Creature summon in GetDamageGuards(manager))
        {
            if (!summon.IsAlive || remaining <= 0m)
                continue;

            bool absorbsWholeHit = summon.HasPower<BufferPower>()
                || summon.HasPower<UndyingMarchPower>();
            decimal damage = absorbsWholeHit ? remaining : decimal.Min(remaining, summon.CurrentHp);
            await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(
                new BlockingPlayerChoiceContext(), summon, damage, routedProps,
                pending.Dealer, pending.CardSource);
            remaining = absorbsWholeHit ? 0m : remaining - damage;
        }

        if (remaining > 0m && Owner.IsAlive)
        {
            await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(
                new BlockingPlayerChoiceContext(),
                Owner,
                remaining,
                routedProps,
                pending.Dealer,
                pending.CardSource);
        }
    }

    private IEnumerable<Creature> GetDamageGuards(RevenantSummonManager manager)
    {
        Creature family = manager.CurrentFamilyCreature;
        var necros = manager.GetLivingNecros().Select(necro => necro.Creature);
        var osties = Owner.Player.PlayerCombatState?.Pets
            .Where(pet => pet.Monster is Osty && pet.PetOwner == Owner.Player && pet != family)
            ?? Enumerable.Empty<Creature>();
        return necros.Concat(osties).Concat(new[] { family })
            .Where(creature => creature is { IsAlive: true }).Distinct().ToArray();
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal unmodifiedCost,
        out decimal modifiedCost)
    {
        if (card is TravelingSatchel &&
            card.Owner == Owner.Player &&
            RevenantSummonManager.For(Owner.Player).CurrentFamilyId == RevenantFamilyId.Helen)
        {
            modifiedCost = System.Math.Max(0m, unmodifiedCost - 1m);
            return true;
        }

        modifiedCost = unmodifiedCost;
        return false;
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext context,
        Player player)
    {
        if (player != Owner.Player)
            return;

        RevenantSummonRelicModel summonRelic = player.Relics
            .OfType<RevenantSummonRelicModel>()
            .FirstOrDefault();
        if (summonRelic != null)
            await summonRelic.PerformInitialCall(context);
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource)
    {
        RevenantSummonManager.For(Owner.Player).RefreshScheduledFamilyIntent();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
            return;

        RevenantSummonManager manager = RevenantSummonManager.For(player);
        await manager.ExecuteScheduledFamilyAction(context);
        await manager.ScheduleFamilyNormalAction(context);
        await manager.TriggerAllNecros(context);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner.Player);
        if (result.WasTargetKilled)
        {
            await manager.HandleFamilyDeath(target);
            return;
        }

        if (manager.IsFamilyCreature(target))
        {
            if (result.TotalDamage > 0m)
                manager.PlayFamilyHitAnimation();
            manager.RefreshScheduledFamilyIntent();
        }
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext context,
        Creature dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel cardSource)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner.Player);
        if ((dealer == Owner || manager.IsFamilyCreature(dealer) || manager.GetLivingNecros().Any(necro => necro.Creature == dealer)) && result.WasTargetKilled)
            manager.TryRegisterNecro(target);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        // Keep the family sprite visible throughout the combat result screen.
        // Its combat scene owns the node and will free it during scene teardown.
        RevenantSummonManager.For(Owner.Player).PrepareForSceneExit();
        RevenantSummonManager.Clear(Owner.Player);
        return Task.CompletedTask;
    }
}
