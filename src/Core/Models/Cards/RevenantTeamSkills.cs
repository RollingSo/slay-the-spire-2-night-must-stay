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
        // Start every participant's native synchronized choice before pausing
        // the action. ReserveChoiceId must run in the deterministic action
        // phase, not after one peer has already completed an earlier choice.
        // Each client opens only its own selection screen; no serial remote
        // wait can prevent another participant's screen from being opened.
        var selectionContext = new BlockingPlayerChoiceContext();
        selectionContext.PushModel(this);
        var choices = new List<Task<IEnumerable<CardModel>>>();
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
        {
            var pile = PileType.Draw.GetPile(player);
            int count = Math.Min(DynamicVars.Cards.IntValue, pile.Cards.Count);
            if (count == 0) continue;
            // Use each recipient as the choice owner so native multiplayer sync
            // asks that player rather than the card's owner to choose.
            MegaCrit.Sts2.Core.Logging.Log.Info($"[SoulDeparture] Request choice: player={player.NetId}, draw={pile.Cards.Count}, count={count}");
            choices.Add(CardSelectCmd.FromCombatPile(selectionContext, pile, player,
                new CardSelectorPrefs(SelectionScreenPrompt, count)));
        }
        if (choices.Count == 0) return;
        CardModel[] cardsToExhaust;
        // Release/Beta differ: newer builds also take the choosing player.
        var beginChoice = typeof(PlayerChoiceContext).GetMethod(nameof(PlayerChoiceContext.SignalPlayerChoiceBegun));
        var options = MegaCrit.Sts2.Core.Entities.Multiplayer.PlayerChoiceOptions.CancelPlayCardActions;
        await (Task)beginChoice.Invoke(context, beginChoice.GetParameters().Length == 2
            ? new object[] { Owner, options } : new object[] { options });
        try
        {
            var results = await Task.WhenAll(choices);
            cardsToExhaust = results.SelectMany(cards => cards).Distinct().ToArray();
        }
        finally
        {
            selectionContext.PopModel(this);
            await context.SignalPlayerChoiceEnded();
        }
        MegaCrit.Sts2.Core.Logging.Log.Info($"[SoulDeparture] All choices complete: selected={cardsToExhaust.Length}");
        // Exhaust hooks can request further choices. Run them only after all
        // recipients have selected, using the original action context as usual.
        foreach (var card in cardsToExhaust)
            if (card.Pile?.Type is PileType.Draw or PileType.Hand or PileType.Discard)
                await CardCmd.Exhaust(context, card);
        MegaCrit.Sts2.Core.Logging.Log.Info("[SoulDeparture] Exhaust complete");
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
