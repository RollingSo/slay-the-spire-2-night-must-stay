using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 75: 校准
public sealed class Calibration : CardModel
{
    private const string MarkKey = "Mark";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new PowerVar<MarkPower>(MarkKey, 1m),
            new CardsVar(1),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/calibration.png");

    public Calibration()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<MarkPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars[MarkKey].BaseValue,
            Owner.Creature,
            this);

        int available = PileType.Draw.GetPile(Owner).Cards.Count(
            card => card.Type == CardType.Attack);
        int count = Math.Min(DynamicVars.Cards.IntValue, available);
        if (count <= 0)
            return;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Draw.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count),
            static card => card.Type == CardType.Attack);
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Cards.UpgradeValueBy(1m);
}

// Card-table ID 76: 穿云箭
public sealed class CloudPiercingArrow : CardModel, ILongShotCard, IMarkTriggerObserver
{
    private const string EnergyKey = "Energy";
    private bool _triggeredMark;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DamageVar(7m, ValueProp.Move),
            new EnergyVar(EnergyKey, 1),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<LongShotPower>(),
            HoverTipFactory.FromPower<DistancePower>(),
            HoverTipFactory.FromPower<MarkPower>(),
            EnergyHoverTip,
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/cloud_piercing_arrow.png");

    public CloudPiercingArrow()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public void OnMarkTriggered(decimal triggeringDamage) =>
        _triggeredMark = true;

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        _triggeredMark = false;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(target =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, target))
            .Execute(choiceContext);

        if (_triggeredMark)
            await PlayerCmd.GainEnergy(DynamicVars[EnergyKey].IntValue, Owner);
    }

    protected override void OnUpgrade() =>
        DynamicVars[EnergyKey].UpgradeValueBy(1m);
}

// Card-table ID 77: 应变
public sealed class Adaptation : CardModel
{
    private const string DistanceKey = "Distance";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new BlockVar(4m, ValueProp.Move),
            new DynamicVar(DistanceKey, 1m),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<DistancePower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/adaptation.png");

    public Adaptation()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool isAttacking = cardPlay.Target.Monster?.NextMove.Intents
            .OfType<AttackIntent>()
            .Any() == true;
        if (!isAttacking)
            return;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DistancePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[DistanceKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Block.UpgradeValueBy(2m);
}
