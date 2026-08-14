using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace sts2mod.Core.Models.Power;

public sealed class PoisonSchemePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeFlushLate(
        PlayerChoiceContext context,
        Player player)
    {
        if (player != Owner.Player || !Hook.ShouldFlush(player.Creature.CombatState, player))
            return;

        var selected = (await CardSelectCmd.FromHand(
                context,
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, Amount),
                card => !card.ShouldRetainThisTurn,
                this))
            .ToList();
        foreach (CardModel card in selected)
            card.GiveSingleTurnRetain();

        await PowerCmd.Remove(this);
    }
}
