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
        ModelDb.Card<IceLightningSpear>(),
        ModelDb.Card<CursedClawCombo>(),
        ModelDb.Card<Halo>(),
        ModelDb.Card<EmergencyRestore>(),
        ModelDb.Card<PreciseLightningStrike>(),
        ModelDb.Card<ThreefoldHalo>(),
        ModelDb.Card<AncientDragonLightning>(),
        ModelDb.Card<LansseaxBlade>(),
        ModelDb.Card<LightningStrike>(),
        ModelDb.Card<AncientDragonSpear>(),
        ModelDb.Card<Recover>(),
        ModelDb.Card<FlannSaxLightningSpear>(),
        ModelDb.Card<BeastClaw>(),
        ModelDb.Card<DeathLightning>(),
        ModelDb.Card<SpaceRendingFrenzy>(),
        ModelDb.Card<WhiteShadowLure>(),
        ModelDb.Card<Soulguard>(),
        ModelDb.Card<LightningSpear>(),
        ModelDb.Card<SpiritForm>(),
        ModelDb.Card<UnbearableFrenzy>(),
        ModelDb.Card<Beaststone>(),
        ModelDb.Card<RadagonHalo>(),
        ModelDb.Card<SoulSummon>(),
        ModelDb.Card<GraveRob>(),
        ModelDb.Card<GreaterRecover>(),
        ModelDb.Card<AncientDragonFaith>(),
        ModelDb.Card<BeastClawMark>(),
        ModelDb.Card<GoldenOrder>(),
        ModelDb.Card<SpiritLink>(),
        ModelDb.Card<BlessingOfGrace>(),
        ModelDb.Card<GurranqBeastClaw>(),
        ModelDb.Card<GurranqsRock>(),
        ModelDb.Card<FrenziedFlame>(),
        ModelDb.Card<Ensemble>(),
        ModelDb.Card<Surge>(),
        ModelDb.Card<UnderworldRising>(),
        ModelDb.Card<Resurgence>(),
        ModelDb.Card<Soulbound>(),
        ModelDb.Card<AnswerTheCall>(),
        ModelDb.Card<RevenantCard>(),
        ModelDb.Card<KingsRecovery>(),
        ModelDb.Card<UndyingMarch>(),
    };
}
