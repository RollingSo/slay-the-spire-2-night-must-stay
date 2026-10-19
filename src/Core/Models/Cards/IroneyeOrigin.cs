using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class IroneyeOrigin : CardModel
{
    // Temporary existing portrait until origin_v1 artwork is approved.
    public override string PortraitPath => "res://images/packed/card_portraits/ironeye/reversal_step.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(5m, ValueProp.Unpowered) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<DistancePower>(), HoverTipFactory.Static(StaticHoverTip.Block)
    };
    public IroneyeOrigin() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<IroneyeOriginPower>(context, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
