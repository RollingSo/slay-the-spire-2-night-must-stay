using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards;

// 冰雷枪：一张会在从弃牌堆回到手牌时逐渐增强冻伤的群体攻击牌。
public sealed class IceLightningSpear : CardModel
{
    private const string FreezeKey = "Freeze";
    private ICombatState _lastCombatState;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<FreezePower>(FreezeKey, 3m),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<FreezePower>(),
    };

    public override string PortraitPath =>
        "res://revenant_assets/cards/ice_lightning_spear.png";

    public IceLightningSpear()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!ReferenceEquals(_lastCombatState, CombatState))
        {
            _lastCombatState = CombatState;
            DynamicVars[FreezeKey].BaseValue = IsUpgraded ? 4m : 3m;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(context);

        Creature[] enemies = CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        if (enemies.Length > 0)
        {
            await PowerCmd.Apply<FreezePower>(
                context,
                enemies,
                DynamicVars[FreezeKey].BaseValue,
                Owner.Creature,
                this);
        }
    }

    public void IncreaseFreezeForCurrentCombat() =>
        DynamicVars[FreezeKey].BaseValue += 1m;

    public override async Task AfterCardPlayedLate(
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        if (cardPlay.Card != this
            || Pile == null
            || Pile.Type == PileType.Hand
            || !Pile.Type.IsCombatPile())
        {
            return;
        }

        // The card returns to hand on the next player turn via a short-lived
        // power.  The extra freeze stack is earned when it is returned.
        await PowerCmd.Apply<IceLightningSpearReturnPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[FreezeKey].UpgradeValueBy(1m);
}

public sealed class CursedClawCombo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(4m, ValueProp.Move),
    };

    public override string PortraitPath =>
        "res://revenant_assets/cards/cursed_claw_combo.png";

    public CursedClawCombo()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(2)
            .Execute(context);

        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count > 0)
            await CardPileCmd.Add(draw.Cards[0], PileType.Discard);
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class Halo : CardModel
{
    private ICombatState _lastCombatState;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("Growth", 2m),
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Ethereal };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
    };

    public override string PortraitPath =>
        "res://revenant_assets/cards/halo.png";

    public Halo()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!ReferenceEquals(_lastCombatState, CombatState))
        {
            _lastCombatState = CombatState;
            DynamicVars.Damage.BaseValue = 7m;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);

        await PowerCmd.Apply<HaloReturnPower>(
            context,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    public void IncreaseDamageForCurrentCombat() =>
        DynamicVars.Damage.BaseValue += DynamicVars["Growth"].BaseValue;

    protected override void OnUpgrade() =>
        DynamicVars["Growth"].UpgradeValueBy(1m);
}
