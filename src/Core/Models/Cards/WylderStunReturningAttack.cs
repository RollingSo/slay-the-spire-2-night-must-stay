using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

// Shared return lifecycle. Concrete cards supply their damage and upgrade variables.
public abstract class WylderStunReturningAttack : WylderCard, IWylderStunReturn
{
    private bool _returnAfterPlay;

    protected WylderStunReturningAttack(int cost, CardRarity rarity)
        : base(cost, CardType.Attack, rarity, TargetType.AnyEnemy) { }

    public async Task OnEnemyStunned()
    {
        if (Owner?.PlayerCombatState == null || Owner.Creature.IsDead || Pile == null || !Pile.Type.IsCombatPile()) return;
        if (Pile.Type == PileType.Play)
        {
            // Preserve the native play/Replay cleanup. Returning now would let
            // that cleanup discard the card again and expire the new discount.
            _returnAfterPlay = true;
            return;
        }
        await ReturnToHand();
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel clonedBy)
    {
        if (card != this || !_returnAfterPlay || Pile == null || Pile.Type == PileType.Play || !Pile.Type.IsCombatPile()) return;
        _returnAfterPlay = false;
        await ReturnToHand();
    }

    private async Task ReturnToHand()
    {
        EnergyCost.SetUntilPlayed(0);
        if (Pile.Type != PileType.Hand) await CardPileCmd.Add(this, PileType.Hand);
    }
}
