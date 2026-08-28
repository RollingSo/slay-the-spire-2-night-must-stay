using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.RelicPools;

public sealed class RevenantRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "revenant";
    public override Color LabOutlineColor => new("67538A");
    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        new RelicModel[]
        {
            ModelDb.Relic<SmallMakeupBrush>(),
            ModelDb.Relic<TreasuredMakeupBrush>(),
            ModelDb.Relic<DirtyPhotoFrame>(),
            ModelDb.Relic<MiniatureMakeupTools>(),
            ModelDb.Relic<DeepSeaNight>(),
            ModelDb.Relic<OldPocketPortrait>(),
            ModelDb.Relic<BlueAmberAmulet>(),
            ModelDb.Relic<BelieversVowCloth>(),
            ModelDb.Relic<PortableSewingKit>(),
        };
}
