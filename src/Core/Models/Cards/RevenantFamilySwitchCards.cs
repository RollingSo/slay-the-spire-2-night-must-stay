using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Cards;

public sealed class MutualUnderstanding : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DynamicVar("Strength", 2m) };
    public override string PortraitPath => "res://revenant_assets/cards/mutual_understanding.png";

    public MutualUnderstanding() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<MutualUnderstandingPower>(context, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}

public sealed class ChangeHands : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/change_hands.png";

    public ChangeHands() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<ChangeHandsPower>(context, Owner.Creature, 1m, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class StunCall : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(8m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/stun_call.png";

    public StunCall() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner);
        RevenantFamilyId? previousFamily = manager.HasLivingFamily ? manager.CurrentFamilyId : null;
        RevenantFamilyId? selectedFamily = await RevenantCall.ChooseFamilyAndCall(context, Owner);
        if (previousFamily.HasValue &&
            previousFamily.Value != RevenantFamilyId.Skeleton &&
            selectedFamily == RevenantFamilyId.Skeleton)
        {
            await RevenantTextTableHelpers.DamageAsFamily(
                this,
                context,
                DynamicVars.Damage.BaseValue,
                all: true);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class Relay : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/relay.png";

    public Relay() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<RelayPower>(context, Owner.Creature, 1m, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class PackUp : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new CardsVar(1) };
    public override string PortraitPath => "res://revenant_assets/cards/pack_up.png";

    public PackUp() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<PackUpPower>(context, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
