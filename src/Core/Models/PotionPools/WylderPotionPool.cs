using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Potions;
namespace NightMustStay.Core.Models.PotionPools;
public sealed class WylderPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "wylder";
    public override Color LabOutlineColor => new("809CC8");
    protected override IEnumerable<PotionModel> GenerateAllPotions() => new PotionModel[]
    { };
}
