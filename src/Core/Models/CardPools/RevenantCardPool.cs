using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Models.CardPools;

public sealed class RevenantCardPool : CardPoolModel
{
    public override string Title => "revenant";
    public override string EnergyColorName => "revenant";
    public override string CardFrameMaterialPath => "card_frame_revenant";
    public override Color DeckEntryCardColor => new("67538A");
    public override Color EnergyOutlineColor => new("21172E");
    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards() => new CardModel[]
    {
        ModelDb.Card<StrikeRevenant>(),
        ModelDb.Card<DefendRevenant>(),
        ModelDb.Card<RevenantCall>(),
        ModelDb.Card<RevenantResonance>(),
    };
}
