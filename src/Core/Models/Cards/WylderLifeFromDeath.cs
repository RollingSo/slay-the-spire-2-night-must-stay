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
        .Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderDesperateSurvival>(IsUpgraded));
}

public sealed class WylderLifeFromDeathPower : WylderAbilityPower
{
    private const int CardsPerCopy = 2;

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
        var choices = ModelDb.CardPool<WylderCardPool>().AllCards.OfType<WylderDesperationCard>().ToArray();
        for (int i = 0; i < total; i++)
        {
            var card = CombatState.CreateCard(player.RunState.Rng.Niche.NextItem(choices), player);
            if (i >= basic) CardCmd.Upgrade(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player));
        }
    }
}
