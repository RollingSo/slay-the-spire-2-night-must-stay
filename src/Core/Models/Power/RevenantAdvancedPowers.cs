using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Revenant;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

public sealed class WhiteShadowLurePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext context,
        Creature target,
        DamageResult result,
        MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner || Amount <= 0m) return;
        if (Amount <= 1m) await PowerCmd.Remove(this);
        else await PowerCmd.ModifyAmount(context, this, -1m, Applier, cardSource);
    }
}

public sealed class SoulguardPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel source)
    {
        if (card?.Owner == Owner.Player &&
            oldPileType == PileType.Discard &&
            card.Pile?.Type == PileType.Hand)
        {
            await CreatureCmd.GainBlock(Owner, Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null);
        }
    }
}

public sealed class SpiritFormPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStartLate(
        PlayerChoiceContext context,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player)
            return;

        for (int i = 0; i < (int)Amount; i++)
        {
            await RevenantSummonManager.For(Owner.Player).IncreaseFamilyMaxHp(6m);
            await RevenantSummonManager.For(Owner.Player).TriggerResonance(context);
        }
    }
}

public sealed class SpiritLinkPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterFamilyCalled()
    {
        Creature family = Owner.Player.Osty;
        if (family is { IsAlive: true })
            await CreatureCmd.GainMaxHp(family, Amount);
    }
}

public sealed class UndyingMarchPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDie(Creature creature) => creature != Owner;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature == Owner && creature.CurrentHp < 1m)
            await CreatureCmd.Heal(creature, 1m, playAnim: false);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Pets are not guaranteed to be included in the side-turn participant
        // collection. Requiring the Family owner to appear there caused this
        // one-turn power to survive indefinitely. Its lifetime is tied to the
        // allied side turn, so the side check alone is the correct boundary.
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}

public sealed class AncientDragonFaithPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner)) return;
        CardPile discard = PileType.Discard.GetPile(Owner.Player);
        int count = System.Math.Min((int)Amount, discard.Cards.Count);
        if (count <= 0)
            return;

        IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            new BlockingPlayerChoiceContext(),
            discard,
            Owner.Player,
            new CardSelectorPrefs(new LocString("cards", "REVENANT_RECOVER_CARDS"), count))).ToArray();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}

public sealed class BeastClawMarkPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterResonance(PlayerChoiceContext context)
    {
        Creature family = Owner.Player.Osty;
        if (family is { IsAlive: true })
            await PowerCmd.Apply<StrengthPower>(context, family, Amount, Owner, null);
    }
}

public sealed class GoldenOrderPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal unmodifiedCost,
        out decimal modifiedCost)
    {
        if (card.Owner != Owner.Player || !card.Keywords.Contains(CardKeyword.Ethereal))
        {
            modifiedCost = unmodifiedCost;
            return false;
        }

        modifiedCost = System.Math.Max(0m, unmodifiedCost - Amount);
        return true;
    }
}

public sealed class BlessingOfGracePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner)) return;
        Creature family = Owner.Player.Osty;
        if (family is { IsAlive: true }) await CreatureCmd.Heal(family, Amount);
    }
}
