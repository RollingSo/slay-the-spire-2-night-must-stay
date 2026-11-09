using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderFlamelightOnSnow : WylderAbilityCard<WylderFlamelightOnSnowPower>
{
    public const int FreezePerCopy = 1;
    public const int VigorPerCopy = 3;

    public WylderFlamelightOnSnow() : base(2, CardRarity.Rare) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<FreezePower>("Freeze", FreezePerCopy),
        new PowerVar<VigorPower>("Vigor", VigorPerCopy)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
    {
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<FreezePower>()
    });
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class WylderFlamelightOnSnowPower : WylderAbilityPower
{
    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("Freeze", WylderFlamelightOnSnow.FreezePerCopy * Copies);
            description.Add("Vigor", WylderFlamelightOnSnow.VigorPerCopy * Copies);
            return description;
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power,
        decimal amount, Creature applier, CardModel cardSource)
    {
        if (amount <= 0 || power is not VigorPower || power.Owner != Owner) return;
        Creature[] enemies = CombatState.GetOpponentsOf(Owner)
            .Where(enemy => enemy.IsAlive && enemy.IsHittable).ToArray();
        if (enemies.Length == 0) return;
        Creature target = Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
        Flash();
        await PowerCmd.Apply<FreezePower>(context, target,
            WylderFlamelightOnSnow.FreezePerCopy * Copies, Owner, null);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature dealer, DamageResult result,
        ValueProp props, Creature target, CardModel cardSource)
    {
        if (dealer != Owner || target == null || target.Side == Owner.Side || result.TotalDamage <= 0
            || !props.IsPoweredAttack() || cardSource?.Type != CardType.Attack
            || !target.HasPower<FreezePower>()) return;
        Flash();
        await PowerCmd.Apply<VigorPower>(context, Owner,
            WylderFlamelightOnSnow.VigorPerCopy * Copies, Owner, cardSource);
    }
}

public sealed class WylderTriumph : WylderCard, IWylderStunReturn
{
    private bool _returnAfterPlay;
    private bool _returnAfterTurnDiscard;

    public WylderTriumph() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

    public async Task OnEnemyStunned()
    {
        if (Owner?.PlayerCombatState == null || Owner.Creature.IsDead || Pile == null || !Pile.Type.IsCombatPile()) return;
        if (WylderGrantedDesperation.IsResolving(Owner))
        {
            // Desperation resolves before the native end-turn hand discard.
            // Returning Triumph immediately would put it into that same discard.
            _returnAfterTurnDiscard = true;
            return;
        }
        if (Pile.Type == PileType.Play)
        {
            _returnAfterPlay = true;
            return;
        }
        await ReturnToHand();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!_returnAfterTurnDiscard || Owner?.PlayerCombatState == null
            || side != CombatSide.Player || !participants.Contains(Owner.Creature)) return;
        _returnAfterTurnDiscard = false;
        if (Pile != null && Pile.Type.IsCombatPile()) await ReturnToHand();
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel clonedBy)
    {
        if (card != this || !_returnAfterPlay || Pile == null || Pile.Type == PileType.Play || !Pile.Type.IsCombatPile()) return;
        _returnAfterPlay = false;
        await ReturnToHand();
    }

    private async Task ReturnToHand()
    {
        if (Pile.Type != PileType.Hand) await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class WylderFatesJudgment : WylderAbilityCard<WylderFatesJudgmentPower>
{
    public const int BaseDebuff = 2;
    public const int UpgradedDebuff = 3;

    public WylderFatesJudgment() : base(1, CardRarity.Rare) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<VulnerablePower>("Vulnerable", BaseDebuff),
        new PowerVar<FreezePower>("Freeze", BaseDebuff)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
    {
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<FreezePower>()
    });
    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(UpgradedDebuff - BaseDebuff);
        DynamicVars["Freeze"].UpgradeValueBy(UpgradedDebuff - BaseDebuff);
    }
}

public sealed class WylderFatesJudgmentPower : WylderAbilityPower
{
    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("Vulnerable", Sum(WylderFatesJudgment.BaseDebuff, WylderFatesJudgment.UpgradedDebuff));
            description.Add("Freeze", Sum(WylderFatesJudgment.BaseDebuff, WylderFatesJudgment.UpgradedDebuff));
            return description;
        }
    }

    internal async Task Trigger(Creature target)
    {
        if (target?.IsAlive != true || target.Side == Owner.Side) return;
        var context = new ThrowingPlayerChoiceContext();
        Flash();
        await PowerCmd.Apply<VulnerablePower>(context, target,
            Sum(WylderFatesJudgment.BaseDebuff, WylderFatesJudgment.UpgradedDebuff), Owner, null);
        await PowerCmd.Apply<FreezePower>(context, target,
            Sum(WylderFatesJudgment.BaseDebuff, WylderFatesJudgment.UpgradedDebuff), Owner, null);
    }
}
