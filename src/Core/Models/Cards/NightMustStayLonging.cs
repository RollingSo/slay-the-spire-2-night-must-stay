using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace NightMustStay.Core.Models.Cards;

// A generated token with the native colorless frame, not a Revenant reward.
public sealed class NightMustStayLonging : CardModel
{
    public NightMustStayLonging() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<ColorlessCardPool>();
    public override string PortraitPath => MissingPortraitPath;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain, CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { GuardianCardHoverTips.RevenantRecover };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        await RevenantCardHelpers.AddFromDiscard(this, context, DynamicVars.Cards.IntValue, false);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);

    internal static async Task Generate(ICombatState combat, Player recipient, bool upgraded, int count, PileType pile)
    {
        for (int i = 0; i < count; i++)
        {
            var card = combat.CreateCard<NightMustStayLonging>(recipient);
            if (upgraded) CardCmd.Upgrade(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, pile, recipient));
        }
    }
}
