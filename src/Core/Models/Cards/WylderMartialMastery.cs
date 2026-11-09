using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderMartialMastery : WylderAbilityCard<WylderMartialMasteryPower>
{
    public WylderMartialMastery() : base(2, CardRarity.Rare) { }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderMartialMasteryPower : WylderAbilityPower
{
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        var draw = PileType.Draw.GetPile(player);
        for (int i = 0; i < Amount; i++)
        {
            if (!draw.Cards.Any(c => c.Type == CardType.Attack)) return;
            var available = draw.Cards.Where(c => c.Type == CardType.Attack).ToList();
            var offered = new System.Collections.Generic.HashSet<MegaCrit.Sts2.Core.Models.CardModel>();
            while (offered.Count < 3 && available.Count > 0) { var pick = player.RunState.Rng.Niche.NextItem(available); available.Remove(pick); offered.Add(pick); }
            var selected = (await CardSelectCmd.FromCombatPile(context, draw, player,
             new CardSelectorPrefs(new LocString("cards", "WYLDER_MARTIAL_MASTERY_SELECT"), 1),
             c => offered.Contains(c))).FirstOrDefault();
            if (selected == null) return;
            Flash();
            await CardPileCmd.Add(selected, PileType.Hand, CardPilePosition.Bottom, this);
        }
    }
}
