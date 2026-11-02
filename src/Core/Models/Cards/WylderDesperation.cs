using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

// Definition and reusable hand condition; no applied power or placeholder icon.
internal static class WylderDesperation
{
    internal static IHoverTip HoverTip => new HoverTip(
        new LocString("cards", "WYLDER_DESPERATION.title"),
        new LocString("cards", "WYLDER_DESPERATION.description"));

    internal static bool IsOnlyCardInHand(CardModel card)
    {
        if (card.Owner?.PlayerCombatState == null) return false;
        var hand = PileType.Hand.GetPile(card.Owner);
        return card.Pile == hand && hand.Cards.Count == 1 && hand.Cards[0] == card;
    }

    internal static async Task AutoPlayTargeted(PlayerChoiceContext context, CardModel card, Creature[] legalTargets)
    {
        if (!IsOnlyCardInHand(card)) return;
        var targets = legalTargets.Where(target => target.IsAlive).ToArray();
        if (targets.Length == 0) return;
        Creature target = card.Owner.RunState.Rng.CombatTargets.NextItem(targets);
        // Native AutoPlay spends zero energy and preserves hooks/history.
        await CardCmd.AutoPlay(context, card, target);
    }

    internal static Task AutoPlayUntargeted(PlayerChoiceContext context, CardModel card) =>
        IsOnlyCardInHand(card) ? CardCmd.AutoPlay(context, card, null) : Task.CompletedTask;
}
