using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Cards;

public sealed class CloseGuard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new BlockVar(7m, ValueProp.Move), new CardsVar(2) };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/close_guard.png";

    public CloseGuard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (RevenantSummonManager.For(Owner).CurrentFamilyId == RevenantFamilyId.Helen)
            await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class BodyguardBone : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar("BonusBlock", 8m),
    };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/bodyguard_bone.png";

    public BodyguardBone() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (RevenantSummonManager.For(Owner).CurrentFamilyId == RevenantFamilyId.Skeleton)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars["BonusBlock"].BaseValue,
                ValueProp.Move,
                cardPlay);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public sealed class TravelingSatchel : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/traveling_satchel.png";

    public TravelingSatchel() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class WrathfulNote : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<WeakPower>("Weak", 1m),
    };
    public override string PortraitPath => "res://revenant_assets/cards/wrathful_note.png";

    public WrathfulNote() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(context);
        if (RevenantSummonManager.For(Owner).CurrentFamilyId == RevenantFamilyId.Skeleton)
        {
            await PowerCmd.Apply<WeakPower>(
                context,
                CombatState.HittableEnemies,
                DynamicVars["Weak"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Weak"].UpgradeValueBy(1m);
}

public sealed class JointStrike : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(5m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/joint_strike.png";

    public JointStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
        if (RevenantSummonManager.For(Owner).CurrentFamilyId == RevenantFamilyId.PumpkinHead)
            await RevenantSummonManager.For(Owner).TriggerResonance(context);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class BruteForcePath : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<VulnerablePower>("Vulnerable", 2m),
    };
    public override string PortraitPath => "res://revenant_assets/cards/brute_force_path.png";

    public BruteForcePath() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
        if (RevenantSummonManager.For(Owner).CurrentFamilyId == RevenantFamilyId.PumpkinHead && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(
                context,
                cardPlay.Target,
                DynamicVars["Vulnerable"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Vulnerable"].UpgradeValueBy(1m);
}
