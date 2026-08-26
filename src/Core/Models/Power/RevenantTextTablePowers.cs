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
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Power;

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
        if (!RevenantSummonManager.For(Owner.Player).IsFamilyCreature(target) || result.UnblockedDamage <= 0) return;
        for (int i = 0; i < result.UnblockedDamage; i++)
        {
            Creature[] enemies = Owner.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            if (enemies.Length == 0) return;
            Creature random = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
            await CreatureCmd.Damage(context, random, Amount, ValueProp.Unpowered, Owner, null);
        }
    }
}

public sealed class FightForMePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner.Player)
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
    public override PowerStackType StackType => PowerStackType.Single;

    public async Task AfterChargedCardPlayed(PlayerChoiceContext context)
    {
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
    public override PowerStackType StackType => PowerStackType.Single;

    public async Task AfterFamilyCalled(PlayerChoiceContext context, RevenantFamilyId? family)
    {
        if (_triggeredThisTurn || family != RevenantFamilyId.Helen) return;
        _triggeredThisTurn = true;
        await CardPileCmd.Draw(context, 1m, Owner.Player);
        await PlayerCmd.GainEnergy(1m, Owner.Player);
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
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Owner,
            5m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner,
            null);
    }
}
