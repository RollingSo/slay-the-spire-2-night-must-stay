using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Potions;

namespace sts2mod.Core.Models.PotionPools
{
    public sealed class GuardianPotionPool : PotionPoolModel
    {
        public override string EnergyColorName => "guardian";
        public override Color LabOutlineColor => StsColors.blue;

        protected override IEnumerable<PotionModel> GenerateAllPotions()
        {
            return new PotionModel[]
            {
                ModelDb.Potion<StalwartShieldGrease>(),
                ModelDb.Potion<KnightlySpirit>(),
                ModelDb.Potion<FearlessLiquor>(),
            };
        }
    }
}
