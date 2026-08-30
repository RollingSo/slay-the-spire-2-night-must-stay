using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.CardPools;

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
        ModelDb.Card<NightMustStay.Core.Models.Cards.Soulbound>(),
        ModelDb.Card<AnswerTheCall>(),
        ModelDb.Card<RevenantCard>(),
        ModelDb.Card<KingsRecovery>(),
        ModelDb.Card<UndyingMarch>(),
        ModelDb.Card<FrenziedThreeFingers>(),
        ModelDb.Card<FormationBreakerHammer>(),
        ModelDb.Card<LifeAndDeath>(),
        ModelDb.Card<GiantSkeletonWrath>(),
        ModelDb.Card<SkyRendingChord>(),
        ModelDb.Card<SubstituteDoll>(),
        ModelDb.Card<SpiritGathering>(),
        ModelDb.Card<Concerto>(),
        ModelDb.Card<FightForMe>(),
        ModelDb.Card<SoulCursingBell>(),
        ModelDb.Card<LightSpirit>(),
        ModelDb.Card<Grooming>(),
        ModelDb.Card<ReanimateDead>(),
        ModelDb.Card<SoulReturn>(),
        ModelDb.Card<HeavyEcho>(),
        ModelDb.Card<ChantingBlessing>(),
        ModelDb.Card<UnderworldReflection>(),
        ModelDb.Card<SpiritManipulation>(),
        ModelDb.Card<PreparationRitual>(),
        ModelDb.Card<WatchfulWaiting>(),
        ModelDb.Card<AllSoulsReturn>(),
        ModelDb.Card<FollowingShadow>(),
        ModelDb.Card<CloseGuard>(),
        ModelDb.Card<BodyguardBone>(),
        ModelDb.Card<TravelingSatchel>(),
        ModelDb.Card<WrathfulNote>(),
        ModelDb.Card<JointStrike>(),
        ModelDb.Card<BruteForcePath>(),
        ModelDb.Card<MutualUnderstanding>(),
        ModelDb.Card<ChangeHands>(),
        ModelDb.Card<StunCall>(),
        ModelDb.Card<Relay>(),
        ModelDb.Card<PackUp>(),
        ModelDb.Card<BurnLife>(),
        ModelDb.Card<SoulChargingClaw>(),
        ModelDb.Card<GazeBeyond>(),
        ModelDb.Card<DeadRealmSpiritFire>(),
        ModelDb.Card<IceLightningSpear>(),
        ModelDb.Card<NecroDrive>(),
        ModelDb.Card<BoneCoin>(),
        ModelDb.Card<Harmony>(),
        ModelDb.Card<GhostlyTouch>(),
    };
}
