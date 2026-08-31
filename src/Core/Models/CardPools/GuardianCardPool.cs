using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.CardPools
{
    public sealed class GuardianCardPool : CardPoolModel
    {
        public override string Title => "guardian";

        public override string EnergyColorName => "guardian";

        public override string CardFrameMaterialPath => "card_frame_guardian";

        public override Color DeckEntryCardColor => new Color("3A6EA5");

        public override Color EnergyOutlineColor => new Color("1A3A5C");

        public override bool IsColorless => false;

        protected override CardModel[] GenerateAllCards()
        {
            CardModel[] guardianCards = new CardModel[]
            {
                ModelDb.Card<GuardCounterCard>(),
                ModelDb.Card<PowerfulGuardCounter>(),
                ModelDb.Card<Engage>(),
                ModelDb.Card<PowerfulDefend>(),
                ModelDb.Card<ProbingStab>(),
                ModelDb.Card<EmergencyDefend>(),
                ModelDb.Card<HoldTheLine>(),
                ModelDb.Card<WhirlingStrike>(),
                ModelDb.Card<DefensiveReinforcement>(),
                ModelDb.Card<AllOutCounter>(),
                ModelDb.Card<BreathingRoom>(),
                ModelDb.Card<WingStrike>(),
                ModelDb.Card<Cyclone>(),
                ModelDb.Card<ProtectiveAirstream>(),
                ModelDb.Card<StepForwardPursuit>(),
                ModelDb.Card<Advance>(),
                ModelDb.Card<StandFirm>(),
                ModelDb.Card<SpearAndShield>(),
                ModelDb.Card<Horn>(),
                ModelDb.Card<HalberdWingCombo>(),
                ModelDb.Card<CounterStep>(),
                ModelDb.Card<FeatherSword>(),
                ModelDb.Card<DesperateStruggle>(),
                ModelDb.Card<WingFlap>(),
                ModelDb.Card<GreatShieldShock>(),
                ModelDb.Card<ShieldPokeTactics>(),
                ModelDb.Card<Topple>(),
                ModelDb.Card<SharedGreatShield>(),
                ModelDb.Card<SaviorForm>(),
                ModelDb.Card<SaviorSpreadWings>(),
                ModelDb.Card<StompStance>(),
                ModelDb.Card<ShieldedThrust>(),

                ModelDb.Card<GuardianPreparation>(),
                ModelDb.Card<CycloneHalberd>(),
                ModelDb.Card<GuardianCharge>(),
                ModelDb.Card<GuardianSkyward>(),
                ModelDb.Card<EvolvedDefend>(),
                ModelDb.Card<SharpenResolve>(),
                ModelDb.Card<GreatTornado>(),

                ModelDb.Card<StormAssault>(),
                ModelDb.Card<InvokeStorm>(),
                ModelDb.Card<PhantomSpear>(),
                ModelDb.Card<PhantomCoStrike>(),
                ModelDb.Card<SlowDefend>(),
                ModelDb.Card<IronWallDefend>(),
                ModelDb.Card<StormAvatar>(),

                ModelDb.Card<GuardianSanctuary>(),
                ModelDb.Card<GuardianAssault>(),
                ModelDb.Card<OffenseDefenseShift>(),
                ModelDb.Card<UltimateOffenseDefense>(),
                ModelDb.Card<SpearPolish>(),
                ModelDb.Card<ThousandWeightHalberd>(),
                ModelDb.Card<WanderingSpellSoul>(),
                ModelDb.Card<StormBirth>(),
                ModelDb.Card<StormKick>(),
                ModelDb.Card<GiantHunter>(),
                ModelDb.Card<ShieldImpact>(),
                ModelDb.Card<StormBarrier>(),
                ModelDb.Card<BladewindConvergence>(),
                ModelDb.Card<SpearGrinding>(),
                ModelDb.Card<EvolutionWings>(),
                ModelDb.Card<DesperateBlow>(),
                ModelDb.Card<StalwartShield>(),
                ModelDb.Card<WardingGale>(),
                ModelDb.Card<AbsoluteDefense>(),
                ModelDb.Card<GuardianWhirlwind>(),
                ModelDb.Card<NightMustStay.Core.Models.Cards.NightMustStaySidestep>(),
                ModelDb.Card<UltimateDefendCounter>(),
                ModelDb.Card<FinalCurtainHalberd>(),
                ModelDb.Card<Fearless>(),
                ModelDb.Card<SwallowReturnWind>(),
                ModelDb.Card<Heavenfall>(),
                ModelDb.Card<RetreatingDefense>(),
                ModelDb.Card<SkySweepingGod>(),
                ModelDb.Card<HeavyHalberd>(),
                ModelDb.Card<Featherstep>(),
                ModelDb.Card<DustReturnSlash>(),
                ModelDb.Card<EveOfCounterattack>(),
                ModelDb.Card<HideAndSeekStab>(),
                ModelDb.Card<CloudRendingSweep>(),
                ModelDb.Card<CirclingGust>(),
                ModelDb.Card<WorldEndingWings>(),

                // These retain their normal rarity bands but are filtered out of
                // singleplayer rewards by CardMultiplayerConstraint.MultiplayerOnly.
                ModelDb.Card<StepForwardForAll>(),
                ModelDb.Card<GuardianMultiplayerCard>(),

                // Ancient rarity keeps these out of ordinary rewards. Their
                // acquisition is wired to Dusty Tome and Archaic Tooth respectively.
                ModelDb.Card<CounterLikeTide>(),
                ModelDb.Card<UnbreakableStance>(),

                // Basic cards do not appear in normal rewards, but they still need a
                // real pool so the compendium and card renderer can resolve CardModel.Pool.
                ModelDb.Card<StrikeGuardian>(),
                ModelDb.Card<DefendGuardian>(),
            };

            return guardianCards;
        }
    }
}
