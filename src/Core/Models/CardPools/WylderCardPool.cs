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
    public override Color EnergyOutlineColor => new("111827");
    public override bool IsColorless => false;
    protected override CardModel[] GenerateAllCards() => new CardModel[] {
        ModelDb.Card<WylderStrike>(),
        ModelDb.Card<WylderDefend>(),
        ModelDb.Card<WylderClawMovement>(),
        ModelDb.Card<WylderDashAttack>(),
        ModelDb.Card<WylderSlidingLeap>(),
        ModelDb.Card<WylderParry>(),
        ModelDb.Card<WylderCriticalStrike>(),
        ModelDb.Card<WylderStampUpwardCut>(),
        ModelDb.Card<WylderStampSweep>(),
        ModelDb.Card<WylderVacuumSlice>(),
        ModelDb.Card<WylderStormBlade>(),
        ModelDb.Card<WylderFlamingStrike>(),
        ModelDb.Card<WylderFlameOfRedmanes>(),
        ModelDb.Card<WylderBloodhoundsFinesse>(),
        ModelDb.Card<WylderLionsClaw>(),
        ModelDb.Card<WylderGhostflameExplosion>(),
        ModelDb.Card<WylderRuinousGhostflame>(),
        ModelDb.Card<WylderEochaidsDancingBlade>(),
        ModelDb.Card<WylderTakersFlames>(),
        ModelDb.Card<WylderDualGreatswordLeap>(),
        ModelDb.Card<WylderFlameChaseSlash>(),
        ModelDb.Card<WylderJumpingHeavy>(),
        ModelDb.Card<WylderAssaultStake>(),
        ModelDb.Card<WylderAerialStake>(),
        ModelDb.Card<WylderCounterRush>(),
        ModelDb.Card<WylderBoldness>(),
        ModelDb.Card<WylderDebut>(),
        ModelDb.Card<WylderSixthSense>(),
        ModelDb.Card<WylderClawPull>(),
        ModelDb.Card<WylderClawRend>(),
        ModelDb.Card<WylderMotionAndStillness>(),
        ModelDb.Card<WylderExecution>(),
        ModelDb.Card<WylderCorpseWaxCutter>(),
        ModelDb.Card<WylderVeteranEdge>(),
        ModelDb.Card<WylderNightForm>(),
        ModelDb.Card<WylderDesperateSurvival>(),
        ModelDb.Card<WylderRemainingEmber>(),
        ModelDb.Card<WylderUndying>(),
        ModelDb.Card<WylderWildfire>(),
    };
}
