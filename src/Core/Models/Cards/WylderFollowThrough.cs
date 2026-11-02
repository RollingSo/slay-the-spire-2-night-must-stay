using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderFollowThroughCard : WylderCard
{
    protected WylderFollowThroughCard(int cost, CardRarity rarity) : base(cost, CardType.Attack, rarity, TargetType.AnyEnemy) { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new IHoverTip[] {
        new HoverTip(new LocString("cards", "WYLDER_FOLLOW_THROUGH.title"), new LocString("cards", "WYLDER_FOLLOW_THROUGH.description")) });

    internal static CardModel[] Snapshot(MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        PileType.Hand.GetPile(player).Cards.Where(c => c is WylderFollowThroughCard).ToArray();

    internal static async Task PlaySnapshot(PlayerChoiceContext context, CardModel[] cards)
    {
        foreach (var card in cards)
        {
            if (card.Pile?.Type != PileType.Hand) continue;
            var enemies = card.CombatState.GetOpponentsOf(card.Owner.Creature).Where(e => e.IsAlive).ToArray();
            if (enemies.Length == 0) break;
            await CardCmd.AutoPlay(context, card, card.Owner.RunState.Rng.CombatTargets.NextItem(enemies));
        }
    }
}
