using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderLifeFromDeath : WylderAbilityCard<WylderLifeFromDeathPower>
{
    public WylderLifeFromDeath() : base(1, CardRarity.Ancient) { }
    protected override void OnUpgrade() { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderDesperateFlurry>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderEndTheNight>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderTacticalRetreat>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderGoldenParry>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderParry>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderCounterRush>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderAssaultStake>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderSixthSense>(IsUpgraded))
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderDesperateSurvival>(IsUpgraded));
}

public sealed class WylderLifeFromDeathPower : WylderAbilityPower
{
    private const int CardsPerCopy = 2;
    private const int RareCardPercent = 3;

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("GeneratedCards", Copies * CardsPerCopy);
            return description;
        }
    }

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player ? Math.Max(0m, count - Amount) : count;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        int basic = BasicCopies * CardsPerCopy;
        int total = Copies * CardsPerCopy;
        Flash();
        var choices = ModelDb.CardPool<WylderCardPool>().AllCards.OfType<WylderDesperationCard>()
            .Where(card => card is not WylderSixthSense and not WylderDesperateSurvival).ToArray();
        var rng = player.RunState.Rng.Niche;
        for (int i = 0; i < total; i++)
        {
            // Every percentage point has one slot per regular choice. This keeps
            // the two fixed probabilities exact with a single RNG call per card.
            int roll = rng.NextInt(100 * choices.Length);
            var model = (roll / choices.Length) switch
            {
                < RareCardPercent => ModelDb.Card<WylderSixthSense>(),
                < 2 * RareCardPercent => ModelDb.Card<WylderDesperateSurvival>(),
                _ => choices[roll % choices.Length]
            };
            var card = CombatState.CreateCard(model, player);
            if (i >= basic) CardCmd.Upgrade(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player));
        }
    }
}
