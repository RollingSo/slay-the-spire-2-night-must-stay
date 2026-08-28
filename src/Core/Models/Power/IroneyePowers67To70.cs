using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

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

public sealed class FrightenedBirdStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<FrightenedBird>();

    protected override bool IsPositive => false;
}
