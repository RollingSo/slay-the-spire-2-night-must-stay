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
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class CursedClawCombo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
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

        await RevenantCardHelpers.DiscardFromDrawTopWithShuffle(this, context, 2);
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
