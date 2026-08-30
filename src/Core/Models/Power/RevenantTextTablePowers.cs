using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Power;

public sealed class FrenziedThreeFingersPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        decimal hpLost = result.UnblockedDamage - result.OverkillDamage;
        if (!RevenantSummonManager.For(Owner.Player).IsKnownFamilyCreature(target) || hpLost <= 0m)
            return;

        Creature[] enemies = Owner.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (enemies.Length == 0)
            return;

        Creature random = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(
            context,
            random,
            hpLost * Amount,
            ValueProp.Unpowered,
            Owner,
            null);
    }
}

public sealed class FightForMePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player)
            return;

        for (int i = 0; i < (int)Amount; i++)
            await RevenantCall.ChooseFamilyAndCall(context, player);
    }
}

public sealed class LightSpiritPower : PowerModel
{
    private bool _triggeredThisTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (_triggeredThisTurn ||
            card?.Owner != Owner.Player ||
            !RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            return;

        _triggeredThisTurn = true;
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side == Owner.Side && creatures.Contains(Owner))
            _triggeredThisTurn = false;
        return Task.CompletedTask;
    }
}

public sealed class HeavyEchoPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterChargedCardPlayed(PlayerChoiceContext context)
    {
        for (int i = 0; i < (int)Amount; i++)
            await RevenantCall.ChooseFamilyAndCall(context, Owner.Player);
    }
}

public sealed class ChantingBlessingPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public Task AfterChargeCompleted() =>
        CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
}

public sealed class FollowingShadowPower : PowerModel
{
    private bool _triggeredThisTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterFamilyCalled(PlayerChoiceContext context, RevenantFamilyId? family)
    {
        if (_triggeredThisTurn || family != RevenantFamilyId.Helen) return;
        _triggeredThisTurn = true;
        await CardPileCmd.Draw(context, Amount, Owner.Player);
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side == Owner.Side && creatures.Contains(Owner))
            _triggeredThisTurn = false;
        return Task.CompletedTask;
    }
}

public sealed class NecromancyPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner) || !Owner.IsAlive) return;
        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(
            new BlockingPlayerChoiceContext(),
            Owner,
            5m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner,
            null);
    }
}
