using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderFlameChaseSlash : WylderCard
{
    public WylderFlameChaseSlash() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new CardsVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(
        HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(IsUpgraded));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        var coating = CombatState.CreateCard<WylderFireCoating>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(coating);
        await CardPileCmd.AddGeneratedCardToCombat(coating, PileType.Hand, Owner);
        Owner.Creature.GetPower<WylderPursuitPower>()?.KeepFor(play);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
