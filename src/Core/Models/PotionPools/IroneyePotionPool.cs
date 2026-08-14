using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Potions;

namespace sts2mod.Core.Models.PotionPools
{
    public sealed class IroneyePotionPool : PotionPoolModel
    {
        public override string EnergyColorName => "ironeye";

        public override Color LabOutlineColor => new("75824D");

        protected override IEnumerable<PotionModel> GenerateAllPotions()
        {
            return new PotionModel[]
            {
                ModelDb.Potion<PoisonGrease>(),
                ModelDb.Potion<ThrownArrowPotion>(),
                ModelDb.Potion<PickledTurtleNeckMeat>(),
            };
        }
    }
}
