using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderDesperationCard : WylderCard
{
    protected WylderDesperationCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(
        new HoverTip(new LocString("cards", "WYLDER_DESPERATION.title"),
            new LocString("cards", "WYLDER_DESPERATION.description")));

    protected virtual IEnumerable<Creature> LegalDesperationTargets() =>
        CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive);

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.PlayerCombatState == null || !participants.Contains(Owner.Creature)) return;
        var hand = PileType.Hand.GetPile(Owner);
        if (hand.Cards.FirstOrDefault() != this || hand.Cards.Any(card => card is not WylderDesperationCard)) return;

        // Draws caused by the first play must not change the original batch.
        foreach (var card in hand.Cards.Cast<WylderDesperationCard>().ToArray())
        {
            if (card.Pile != hand) continue;
            if (!CombatState.GetOpponentsOf(Owner.Creature).Any(enemy => enemy.IsAlive)) break;
            Creature target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                var targets = card.LegalDesperationTargets().ToArray();
                if (targets.Length == 0) continue;
                target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
            }
            await CardCmd.AutoPlay(context, card, target);
        }
    }
}
