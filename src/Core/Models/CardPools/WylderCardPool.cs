using Godot;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
namespace NightMustStay.Core.Models.CardPools;
public sealed class WylderCardPool : CardPoolModel
{
    public override string Title => "wylder";
    public override string EnergyColorName => "wylder";
    public override string CardFrameMaterialPath => "card_frame_wylder";
    public override Color DeckEntryCardColor => NightMustStay.Core.CharacterThemeColors.Wylder;
    public override Color EnergyOutlineColor => new("17243D");
    public override bool IsColorless => false;
    protected override CardModel[] GenerateAllCards() => new CardModel[] {
        ModelDb.Card<WylderStrike>(),
        ModelDb.Card<WylderDefend>(),
        ModelDb.Card<WylderClawMovement>(),
        ModelDb.Card<WylderDashAttack>(),
        ModelDb.Card<WylderSlidingLeap>(),
        ModelDb.Card<WylderStampUpwardCut>(),
        ModelDb.Card<WylderStampSweep>(),
    };
}
