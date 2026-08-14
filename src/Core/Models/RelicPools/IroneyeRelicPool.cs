using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Relics;

namespace sts2mod.Core.Models.RelicPools
{
    public sealed class IroneyeRelicPool : RelicPoolModel
    {
        public override string EnergyColorName => "ironeye";

        public override Color LabOutlineColor => new("75824D");

        protected override IEnumerable<RelicModel> GenerateAllRelics()
        {
            return new RelicModel[]
            {
                ModelDb.Relic<CursemarkSignet>(),
                ModelDb.Relic<RunemarkSignet>(),
                ModelDb.Relic<CrackedSealingWax>(),
                ModelDb.Relic<WisdomsDarkNight>(),
                ModelDb.Relic<ProtectiveScaleArmor>(),
                ModelDb.Relic<FarArrowTalisman>(),
                ModelDb.Relic<HardArrowTalisman>(),
                ModelDb.Relic<SacredRhythmBlade>(),
                ModelDb.Relic<GlidingGarb>(),
            };
        }
    }
}
