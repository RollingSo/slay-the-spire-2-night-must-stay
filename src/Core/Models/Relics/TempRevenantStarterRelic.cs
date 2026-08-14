using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Relics;

// The hidden controller power connects the character's summon lifecycle.
public sealed class TempRevenantStarterRelic : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => "res://revenant_assets/relics/revenant_starter_relic.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;

    public override Task BeforeCombatStart() => PowerCmd.Apply<RevenantSummonControllerPower>(
        new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
}
