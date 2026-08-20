using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Relics;

namespace sts2mod.Core.Models.RelicPools;

public sealed class RevenantRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "revenant";
    public override Color LabOutlineColor => new("67538A");
    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        new[] { ModelDb.Relic<SmallMakeupBrush>() };
}
