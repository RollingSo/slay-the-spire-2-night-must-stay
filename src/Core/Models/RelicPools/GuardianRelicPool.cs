using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.RelicPools
{
    public sealed class GuardianRelicPool : RelicPoolModel
    {
        public override string EnergyColorName => "guardian";

        public override Color LabOutlineColor => StsColors.blue;

        protected override IEnumerable<RelicModel> GenerateAllRelics()
        {
            return new RelicModel[]
            {
                ModelDb.Relic<SingleWingGreatshield>(),
                ModelDb.Relic<TwinWingGreatshield>(),
                ModelDb.Relic<HuntersDarkNight>(),
                ModelDb.Relic<FlyingFeatherHelm>(),
                ModelDb.Relic<StonePillar>(),
                ModelDb.Relic<WitchBrooch>(),
                ModelDb.Relic<GreenTalisman>(),
                ModelDb.Relic<GreatshieldTalisman>(),
                ModelDb.Relic<TacticalCompendium>(),
            };
        }
    }
}
