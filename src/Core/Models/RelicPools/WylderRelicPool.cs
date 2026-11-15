using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Relics;
namespace NightMustStay.Core.Models.RelicPools;
public sealed class WylderRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "wylder";
    public override Color LabOutlineColor => new("809CC8");
    protected override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[]
    {
        ModelDb.Relic<WylderSingleEarring>(),
        ModelDb.Relic<WylderEarring>(),
        ModelDb.Relic<WylderDarkGrayWhetstone>(),
        ModelDb.Relic<WylderSearingSwordOil>(),
        ModelDb.Relic<WylderGodfreyPortrait>(),
        ModelDb.Relic<WylderSilverTeardrop>(),
        ModelDb.Relic<WylderKingsNight>(),
        ModelDb.Relic<WylderWarriorJarShard>(),
        ModelDb.Relic<WylderRedStainedBlade>(),
    };
}
