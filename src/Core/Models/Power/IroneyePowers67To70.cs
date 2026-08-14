using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Models.Power;

public sealed class AirRendingArrowStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<AirRendingArrow>();

    protected override bool IsPositive => false;
}

public sealed class ImposingPresenceStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<ImposingPresence>();

    protected override bool IsPositive => false;
}
