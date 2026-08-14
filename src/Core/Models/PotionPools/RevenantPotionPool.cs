using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace sts2mod.Core.Models.PotionPools;

public sealed class RevenantPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "revenant";
    public override Color LabOutlineColor => new("67538A");
    protected override IEnumerable<PotionModel> GenerateAllPotions() => new PotionModel[0];
}
