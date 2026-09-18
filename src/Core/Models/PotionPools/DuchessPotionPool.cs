using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Potions;
namespace NightMustStay.Core.Models.PotionPools;
public sealed class DuchessPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "duchess";
    public override Color LabOutlineColor => new("809CC8");
    protected override IEnumerable<PotionModel> GenerateAllPotions() => new PotionModel[]
    { ModelDb.Potion<DuchessSilverPerfume>(), ModelDb.Potion<DuchessVeilVial>(), ModelDb.Potion<DuchessMemoryDraught>() };
}
