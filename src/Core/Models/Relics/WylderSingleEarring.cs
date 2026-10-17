using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Relics;
public sealed class WylderSingleEarring : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => "res://wylder_assets/relics/wylder_single_earring.png";
    protected override string PackedIconOutlinePath => PackedIconPath.Replace(".png", "_outline.png");
    protected override string BigIconPath => PackedIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WylderPursuitPower>() };
    public override async Task BeforeCombatStart()
    {
        Flash();
        await WylderPursuitPower.Enter(new BlockingPlayerChoiceContext(), Owner.Creature, null);
    }
}
