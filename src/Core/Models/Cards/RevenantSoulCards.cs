using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Cards;

public sealed class BurnLife : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
        new EnergyVar(2),
    };

    public override bool GainsBlock => true;
    protected override bool IsPlayable => RevenantSummonManager.For(Owner).HasLivingFamily;
    public override string PortraitPath => "res://revenant_assets/cards/burn_life.png";

    public BurnLife() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        Creature family = Owner.Osty;
        if (family is not { IsAlive: true })
            return;

        await RevenantCardHelpers.DamageFamily(this, context, family.CurrentHp);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class SoulChargingClaw : CardModel, IRevenantChargeCard
{
    private bool _chargeComplete;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("ChargeDamage", 6m),
        new PowerVar<WeakPower>("Weak", 2m),
        new BoolVar("Ready"),
    };

    public bool IsChargeComplete => _chargeComplete;
    public override string PortraitPath => "res://revenant_assets/cards/soul_charging_claw.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        new CardHoverTip(CreateOppositeChargePreview()),
    };

    public SoulChargingClaw() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete, state =>
            state.Add("ChargedDamage", DynamicVars.Damage.BaseValue + DynamicVars["ChargeDamage"].BaseValue));

    private SoulChargingClaw CreateOppositeChargePreview()
    {
        var preview = (SoulChargingClaw)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }

    private void SetChargePreviewState(bool complete)
    {
        _chargeComplete = complete;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (!_chargeComplete && cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        bool wasCharged = _chargeComplete;
        decimal damage = DynamicVars.Damage.BaseValue
            + (wasCharged ? DynamicVars["ChargeDamage"].BaseValue : 0m);
        _chargeComplete = false;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;

        await DamageCmd.Attack(damage).CompatFromCard(this).Targeting(cardPlay.Target).Execute(context);
        if (wasCharged && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<WeakPower>(
                context,
                cardPlay.Target,
                DynamicVars["Weak"].BaseValue,
                Owner.Creature,
                this);
            await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
        }
    }

    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (_chargeComplete)
            return;
        _chargeComplete = true;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted(this);
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class GazeBeyond : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>("Vulnerable", 1m),
        new PowerVar<WeakPower>("Weak", 1m),
    };

    public override string PortraitPath => "res://revenant_assets/cards/gaze_beyond.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
    };

    public GazeBeyond() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(
            context,
            CombatState.HittableEnemies,
            DynamicVars["Vulnerable"].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<WeakPower>(
            context,
            CombatState.HittableEnemies,
            DynamicVars["Weak"].BaseValue,
            Owner.Creature,
            this);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source) =>
        card == this
            ? RevenantCardHelpers.AutoPlayWhenRecovered(this, oldPileType)
            : Task.CompletedTask;

    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}
