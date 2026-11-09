using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderMagnanimity : WylderCard
{
 public WylderMagnanimity() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
 protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderParry>());
 protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var candidates = PileType.Deck.GetPile(Owner).Cards.Where(c => c is WylderDefend && c.IsTransformable).ToList();
        if (candidates.Count == 0) return;
        var deckStrike = (await CardSelectCmd.FromSimpleGrid(context, candidates, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();
        if (deckStrike == null) return;
        var copies = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play }
            .SelectMany(p => p.GetPile(Owner).Cards).Where(c => c.DeckVersion == deckStrike).ToArray();
        var selected = copies.FirstOrDefault();
        WylderParry deckSlicer = Owner.RunState.CreateCard<WylderParry>(Owner);
        WylderParry combatSlicer = CombatState.CreateCard<WylderParry>(Owner);
        if(selected?.IsUpgraded == true || deckStrike.IsUpgraded) { CardCmd.Upgrade(deckSlicer); CardCmd.Upgrade(combatSlicer); }

        var deckResult = await CardCmd.Transform(deckStrike, deckSlicer, CardPreviewStyle.None);
        if (deckResult == null) return;
        if (deckResult.Value.cardAdded.IsUpgraded && !combatSlicer.IsUpgraded)
            CardCmd.Upgrade(combatSlicer);
        combatSlicer.DeckVersion = deckResult.Value.cardAdded;
        foreach (var copy in copies)
        {
            var replacement = CombatState.CreateCard<WylderParry>(Owner);
            if (copy.IsUpgraded || deckResult.Value.cardAdded.IsUpgraded) CardCmd.Upgrade(replacement);
            replacement.DeckVersion = deckResult.Value.cardAdded;
            await CardCmd.Transform(copy, replacement);
        }
    }

}
