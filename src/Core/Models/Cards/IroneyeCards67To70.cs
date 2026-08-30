using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards;

// Card-table ID 67: 谢幕
public sealed class CurtainCall : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(5m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/curtain_call.png");

    public CurtainCall()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        int retainedCards = PileType.Hand.GetPile(Owner).Cards.Count(
            card => card.ShouldRetainThisTurn);

        for (int i = 0; i < retainedCards; i++)
        {
            Creature[] enemies = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive)
                .ToArray();
            if (enemies.Length == 0)
                break;

            Creature target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(target)
                .WithHitVfxNode(hit =>
                    NightreignHitVfx.CreateIroneyeShot(Owner.Creature, hit))
                .Execute(context);
        }

        PlayerCmd.EndTurn(Owner, false, null);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(3m);
}

// Card-table ID 68: 锐不可当
public sealed class AirRendingArrow : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(6m, ValueProp.Move) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
            HoverTipFactory.FromPower<StrengthPower>(),
        };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Ethereal };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/irresistible_force.png");

    public AirRendingArrow()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .CompatFromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode(hit =>
                NightreignHitVfx.CreateIroneyeShot(Owner.Creature, hit))
            .Execute(context);

        decimal damageDealt = attack.Results
            .SelectMany(resultSet => resultSet)
            .Sum(result => result.TotalDamage);
        if (damageDealt > 0m && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<AirRendingArrowStrengthDownPower>(
                context,
                cardPlay.Target,
                damageDealt,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(3m);
}

// Card-table ID 69: 不怒自威
public sealed class ImposingPresence : CardModel
{
    private const string MarkKey = "Mark";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<MarkPower>(MarkKey, 1m) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.FromPower<StrengthPower>(),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/imposing_presence.png");

    public ImposingPresence()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        await PowerCmd.Apply<MarkPower>(
            context,
            enemies,
            DynamicVars[MarkKey].BaseValue,
            Owner.Creature,
            this);

        foreach (Creature enemy in enemies.Where(enemy => enemy.IsAlive))
        {
            decimal markAmount = enemy.GetPower<MarkPower>()?.Amount ?? 0m;
            if (markAmount <= 0m)
                continue;

            await PowerCmd.Apply<ImposingPresenceStrengthDownPower>(
                context,
                enemy,
                markAmount,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars[MarkKey].UpgradeValueBy(1m);
}

// Card-table ID 70: 看破
public sealed class SeeThrough : CardModel
{
    private const string MarkKey = "Mark";

    protected override bool HasEnergyCostX => true;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new PowerVar<MarkPower>(MarkKey, 1m),
            new BlockVar(4m, ValueProp.Move),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new IHoverTip[]
        {
            HoverTipFactory.FromPower<MarkPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
        };

    public override string PortraitPath =>
        ImageHelper.GetImagePath("packed/card_portraits/ironeye/see_through.png");

    public SeeThrough()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int xValue = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (xValue <= 0)
            return;

        // Resolve this as X separate applications. This preserves the card's
        // intended "X times 1 Mark / X times 4 Block" wording and makes every
        // block application pass through the normal BlockVar path, including
        // Dexterity and other block modifiers.
        for (int i = 0; i < xValue; i++)
        {
            await PowerCmd.Apply<MarkPower>(
                context,
                cardPlay.Target,
                DynamicVars[MarkKey].BaseValue,
                Owner.Creature,
                this);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
