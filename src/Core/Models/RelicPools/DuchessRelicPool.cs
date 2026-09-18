using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Relics;
namespace NightMustStay.Core.Models.RelicPools;
public sealed class DuchessRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "duchess";
    public override Color LabOutlineColor => new("809CC8");
    protected override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[]
    {
        ModelDb.Relic<DuchessOldPocketwatch>(), ModelDb.Relic<DuchessMendedPocketwatch>(),
        ModelDb.Relic<DuchessLaceCuff>(), ModelDb.Relic<DuchessSilverThimble>(),
        ModelDb.Relic<DuchessDanceShoes>(), ModelDb.Relic<DuchessUnsentLetter>(), ModelDb.Relic<DuchessBlueRibbon>(),
    };
}
