using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Relics;

public abstract class DuchessRelic : RelicModel
{
    public override string PackedIconPath => $"res://duchess_assets/relics/{Id.Entry.ToLowerInvariant()}.png";
    protected override string PackedIconOutlinePath => PackedIconPath.Replace(".png", "_outline.png");
    protected override string BigIconPath => PackedIconPath;
}

public class DuchessOldPocketwatch : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public virtual async Task OnMomentTwo(PlayerChoiceContext context)
    {
        Flash();
        await CardPileCmd.Draw(context, 1m, Owner);
    }
}

public sealed class DuchessReversePocketwatch : DuchessOldPocketwatch
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task OnMomentTwo(PlayerChoiceContext context)
    {
        Flash();
        await CardPileCmd.Draw(context, 1m, Owner);
        await PlayerCmd.GainEnergy(1m, Owner);
    }
}

public sealed class DuchessCrownBadge : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public async Task OnMomentFive(PlayerChoiceContext context)
    {
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (enemies.Length == 0) return;
        Flash();
        await CreatureCmd.Damage(context, enemies, 5m, ValueProp.Unpowered, Owner.Creature);
    }
}

public sealed class DuchessGoldenDewdrop : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature)) return;
        int moment = DuchessMomentPower.Current(Owner);
        if (moment == 0) return;
        Creature enemy = Owner.RunState.Rng.CombatTargets.NextItem(
            Owner.Creature.CombatState.HittableEnemies.Where(target => target.IsAlive).ToArray());
        if (enemy == null) return;
        Flash();
        await CreatureCmd.Damage(context, enemy, moment, ValueProp.Unpowered, Owner.Creature);
    }
}

public sealed class DuchessPrimalGlintstoneBlade : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.PlayerCombatState.TurnNumber % 4 != 0) return;
        Flash();
        DuchessRadiantBlade blade = Owner.Creature.CombatState.CreateCard<DuchessRadiantBlade>(Owner);
        if (Owner.Creature.GetPower<DuchessRadiantBladeGrowthPower>() is { } growth)
            blade.DynamicVars.Damage.BaseValue += growth.TotalGrowth;
        await CardPileCmd.AddGeneratedCardToCombat(blade, PileType.Hand, Owner, CardPilePosition.Top);
    }
}

public sealed class DuchessBlessedIronCoin : DuchessRelic
{
    [SavedProperty]
    public bool DexterityActive { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource)
    {
        if (power is not DuchessConcealmentPower || power.Owner != Owner.Creature) return;
        bool active = power.Amount > 0m;
        if (active == DexterityActive) return;
        DexterityActive = active;
        Flash();
        await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, active ? 2m : -2m, Owner.Creature, cardSource);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        DexterityActive = false;
        return Task.CompletedTask;
    }
}

public sealed class DuchessNightOfWisdom : DuchessRelic
{
    [SavedProperty]
    public int LastTriggeredTurn { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner || play.Card is not DuchessCard { HasReaction: true }
            || LastTriggeredTurn == Owner.PlayerCombatState.TurnNumber) return;
        LastTriggeredTurn = Owner.PlayerCombatState.TurnNumber;
        Flash();
        await CardPileCmd.Draw(context, 1m, Owner);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        LastTriggeredTurn = -1;
        return Task.CompletedTask;
    }
}

public sealed class DuchessBlueStainedBlade : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || !Owner.Creature.HasPower<DuchessConcealmentPower>()) return;
        Flash();
        await CardPileCmd.Draw(context, 1m, Owner);
    }
}

public sealed class DuchessCarianBadge : DuchessRelic
{
    [SavedProperty]
    public int MomentEffectsTriggered { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public async Task OnMomentEffect(PlayerChoiceContext context)
    {
        MomentEffectsTriggered++;
        if (MomentEffectsTriggered % 4 != 0) return;
        Flash();
        await PlayerCmd.GainEnergy(1m, Owner);
    }
}
