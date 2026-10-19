using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

public sealed class SoulDeparture : CardModel
{
    public SoulDeparture() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://revenant_assets/cards/soul_departure.png";
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // This action gathers choices from multiple owners. Pausing only the
        // caster's queue would let recipients play cards while their draw-pile
        // choice is pending, including opening another selection screen and
        // reserving competing choice IDs. Keep this group selection atomic;
        // CardSelectCmd still synchronizes each recipient's choice normally.
        var selectionContext = new BlockingPlayerChoiceContext();
        selectionContext.PushModel(this);
        var cardsToExhaust = new List<CardModel>();
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
        {
            var pile = PileType.Draw.GetPile(player);
            int count = Math.Min(DynamicVars.Cards.IntValue, pile.Cards.Count);
            if (count == 0) continue;
            // Use each recipient as the choice owner so native multiplayer sync
            // asks that player rather than the card's owner to choose.
            var selected = (await CardSelectCmd.FromCombatPile(selectionContext, pile, player,
                new CardSelectorPrefs(SelectionScreenPrompt, count))).ToArray();
            cardsToExhaust.AddRange(selected.Where(card => card.Pile == pile));
        }
        // Exhaust hooks can request further choices. Run them only after all
        // recipients have selected, using the original action context as usual.
        foreach (var card in cardsToExhaust)
            if (card.Pile?.Type == PileType.Draw) await CardCmd.Exhaust(context, card);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public sealed class WeepingStrings : CardModel
{
    public WeepingStrings() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://revenant_assets/cards/weeping_strings.png";
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<NightMustStayLonging>(IsUpgraded);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
            await NightMustStayLonging.Generate(CombatState, player, IsUpgraded, 1, PileType.Hand);
    }
    protected override void OnUpgrade() { }
}
