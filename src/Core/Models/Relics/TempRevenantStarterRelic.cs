using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Models.Relics;

// The hidden controller power connects the character's summon lifecycle.
public sealed class SmallMakeupBrush : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool SpawnsPets => true;
    public override string PackedIconPath => "res://revenant_assets/relics/revenant_starter_relic.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new HoverTip(
            new LocString("cards", "REVENANT_CALL.tooltipTitle"),
            new LocString("cards", "REVENANT_CALL.tooltipDescription")),
    };

    public override async Task BeforeCombatStart()
    {
        var context = new BlockingPlayerChoiceContext();
        await PowerCmd.Apply<RevenantSummonControllerPower>(
            context, Owner.Creature, 1m, Owner.Creature, null);
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
    }
}
