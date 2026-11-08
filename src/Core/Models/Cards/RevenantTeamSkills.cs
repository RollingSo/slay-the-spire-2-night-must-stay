using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

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
        // Each recipient needs their own native hook action. Do not pause the
        // played card while waiting for choices owned by other player queues.
        // The synchronous part of SelectAndExhaust reserves the choice ID;
        // AssignTaskAndWaitForPauseOrCompletion then hands off its continuation.
        ulong localPlayerId = LocalContext.NetId ?? Owner.NetId;
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
        {
            var pile = PileType.Draw.GetPile(player);
            int count = Math.Min(DynamicVars.Cards.IntValue, pile.Cards.Count);
            if (count == 0) continue;
            var recipientContext = new HookPlayerChoiceContext(
                player, localPlayerId, GameActionType.CombatPlayPhaseOnly);
            if (!TestMode.IsOn)
                MegaCrit.Sts2.Core.Logging.Log.Info($"[SoulDeparture] Queue owner choice: player={player.NetId}, draw={pile.Cards.Count}, count={count}");
            Task choice = SelectAndExhaust(recipientContext, player, count);
            await recipientContext.AssignTaskAndWaitForPauseOrCompletion(choice);
        }
    }

    private async Task SelectAndExhaust(HookPlayerChoiceContext context, Player player, int count)
    {
        context.PushModel(this);
        try
        {
            CardModel[] selected = (await CardSelectCmd.FromCombatPile(
                context, PileType.Draw.GetPile(player), player,
                new CardSelectorPrefs(SelectionScreenPrompt, count))).Distinct().ToArray();
            foreach (CardModel card in selected)
                if (card.Owner == player && card.Pile?.Type is PileType.Draw or PileType.Hand or PileType.Discard)
                    await CardCmd.Exhaust(context, card);
            if (!TestMode.IsOn)
                MegaCrit.Sts2.Core.Logging.Log.Info($"[SoulDeparture] Owner choice complete: player={player.NetId}, selected={selected.Length}");
        }
        finally { context.PopModel(this); }
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
