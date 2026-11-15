using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderLifeFromDeath : WylderAbilityCard<WylderLifeFromDeathPower>
{
    public WylderLifeFromDeath() : base(0, CardRarity.Ancient) { }
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
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<WylderLifeFromDeathPower>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power == null) return;
        if (IsUpgraded) power.PendingUpgraded += 2;
        else power.PendingBasic += 2;
    }
}

public sealed class WylderLifeFromDeathPower : WylderAbilityPower
{
    [SavedProperty] public int PendingBasic { get; set; }
    [SavedProperty] public int PendingUpgraded { get; set; }
    [SavedProperty] public int DueBasic { get; set; }
    [SavedProperty] public int DueUpgraded { get; set; }
    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player ? Math.Max(0m, count - Amount) : count;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            DueBasic += PendingBasic;
            DueUpgraded += PendingUpgraded;
            PendingBasic = PendingUpgraded = 0;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player) return;
        int basic = DueBasic, upgraded = DueUpgraded;
        DueBasic = DueUpgraded = 0;
        if (basic + upgraded == 0) return;
        Flash();
        var choices = ModelDb.CardPool<WylderCardPool>().AllCards.OfType<WylderDesperationCard>().ToArray();
        for (int i = 0; i < basic + upgraded; i++)
        {
            var card = CombatState.CreateCard(player.RunState.Rng.Niche.NextItem(choices), player);
            if (i >= basic) CardCmd.Upgrade(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player));
        }
    }
}
