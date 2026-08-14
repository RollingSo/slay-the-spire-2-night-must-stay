using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using sts2mod.Core.Models.Cards;

namespace sts2mod.Core.Models.CardPools
{
    public sealed class IroneyeCardPool : CardPoolModel
    {
        public override string Title => "ironeye";

        public override string EnergyColorName => "ironeye";

        public override string CardFrameMaterialPath => "card_frame_ironeye";

        public override Color DeckEntryCardColor => new("75824D");

        public override Color EnergyOutlineColor => new("29351F");

        public override bool IsColorless => false;

        protected override CardModel[] GenerateAllCards()
        {
            return new CardModel[]
            {
                ModelDb.Card<StrikeIroneye>(),
                ModelDb.Card<DefendIroneye>(),
                ModelDb.Card<IroneyeMark>(),
                ModelDb.Card<FullDraw>(),
                ModelDb.Card<ContinuousShooting>(),
                ModelDb.Card<VenomDagger>(),
                ModelDb.Card<BackstepShot>(),
                ModelDb.Card<FingerSnap>(),
                ModelDb.Card<PoisonBurst>(),
                ModelDb.Card<TwinKissPoisonMoth>(),
                ModelDb.Card<AntiAirShot>(),
                ModelDb.Card<SpiritShot>(),
                ModelDb.Card<TripleVolley>(),
                ModelDb.Card<PiercingShot>(),
                ModelDb.Card<GroundSkid>(),
                ModelDb.Card<HunterStepMark>(),
                ModelDb.Card<EvasiveArrowSlash>(),
                ModelDb.Card<IroneyeShadowAssault>(),
                ModelDb.Card<IroneyeHeadshot>(),
                ModelDb.Card<MisdirectionStep>(),
                ModelDb.Card<IroneyeArrowRain>(),
                ModelDb.Card<IroneyePoisonArrow>(),
                ModelDb.Card<PierceTheWillow>(),
                ModelDb.Card<HeartpiercingArrow>(),
                ModelDb.Card<DisorderlyArrows>(),
                ModelDb.Card<StartledBird>(),
                ModelDb.Card<EagleEye>(),
                ModelDb.Card<LightningArrowhead>(),
                ModelDb.Card<BowLikeFullMoon>(),
                ModelDb.Card<BladeShadowUnmatched>(),
                ModelDb.Card<CirclingManeuver>(),
                ModelDb.Card<WaveringStep>(),
                ModelDb.Card<KillingIntentGaze>(),
                ModelDb.Card<ReturnToZero>(),
                ModelDb.Card<RetreatStep>(),
                ModelDb.Card<WitheringSlash>(),
                ModelDb.Card<PoisonMistArrowArray>(),
                ModelDb.Card<BowCombatArt>(),
                ModelDb.Card<BladeGlide>(),
                ModelDb.Card<StarPlucker>(),
                ModelDb.Card<Scouting>(),
                ModelDb.Card<PoisonBlade>(),
                ModelDb.Card<Aim>(),
                ModelDb.Card<ApproachingVenomFang>(),
                ModelDb.Card<AllThingsWither>(),
                ModelDb.Card<AdvanceAndRetreat>(),
                ModelDb.Card<Vigilance>(),
                ModelDb.Card<RoadAlreadyTraveled>(),
                ModelDb.Card<HeavenlyEyeForm>(),
                ModelDb.Card<SharedIntelligence>(),
                ModelDb.Card<IronEye>(),
                ModelDb.Card<Observation>(),

                ModelDb.Card<HuntingPrelude>(),
                ModelDb.Card<Hunt>(),
                ModelDb.Card<WaveWalking>(),
                ModelDb.Card<ArrowOnString>(),
                ModelDb.Card<WitherAndFlourish>(),
                ModelDb.Card<ThroatSeal>(),
                ModelDb.Card<NowhereToHide>(),
                ModelDb.Card<WillowPiercingArrow>(),
                ModelDb.Card<VolatilePoison>(),
                ModelDb.Card<TrackingArrow>(),
                ModelDb.Card<IroneyeSwift>(),
                ModelDb.Card<WitheringCut>(),
                ModelDb.Card<LuckyArrowBag>(),
                ModelDb.Card<PoisonScheme>(),
                ModelDb.Card<CurtainCall>(),
                ModelDb.Card<AirRendingArrow>(),
                ModelDb.Card<ImposingPresence>(),
                ModelDb.Card<SeeThrough>(),
                ModelDb.Card<Skybreaker>(),
                ModelDb.Card<FatalBladeEdge>(),
                ModelDb.Card<Release>(),
                ModelDb.Card<EmergencyNocking>(),
                ModelDb.Card<Calibration>(),
                ModelDb.Card<CloudPiercingArrow>(),
                ModelDb.Card<Adaptation>(),
                ModelDb.Card<Offensive>(),
                ModelDb.Card<ReturningWindArrow>(),
                ModelDb.Card<ReversalStep>(),
                ModelDb.Card<TurningArrow>(),
                ModelDb.Card<SoulChasingVolley>(),
                ModelDb.Card<CorrodeAll>(),
                ModelDb.Card<HundredSchemes>(),
                ModelDb.Card<CutThroughChaos>(),
                ModelDb.Card<GracefulBladeDance>(),

                // Ancient rarity keeps these out of ordinary rewards. Death
                // Mark is excluded from Dusty Tome by Archaic Tooth's original
                // transcendence map, leaving Final Battle as Darv's tome card.
                ModelDb.Card<DeathMark>(),
                ModelDb.Card<FinalBattle>(),
            };
        }
    }
}
