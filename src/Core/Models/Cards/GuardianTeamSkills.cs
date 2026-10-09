using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class TurnTheOffensive : CardModel
{
    // Use the engine's safe missing-art tile until dedicated artwork is approved.
    public override string PortraitPath => "res://images/packed/card_portraits/guardian/turn_the_offensive.png";
    public TurnTheOffensive() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<GuardCounterPower>("GuardCounter", 12m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
            await PowerCmd.Apply<GuardCounterPower>(context, player.Creature, DynamicVars["GuardCounter"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["GuardCounter"].UpgradeValueBy(4m);
}

public sealed class SacredFeatherFormation : CardModel
{
    public override string PortraitPath => "res://images/packed/card_portraits/guardian/sacred_feather_formation.png";
    public SacredFeatherFormation() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("DrawReduction", 2m),
        new PowerVar<RegenPower>("Regen", 4m)
    };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<RegenPower>() };

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SacredFeatherDrawReductionPower>(context, Owner.Creature,
            DynamicVars["DrawReduction"].BaseValue, Owner.Creature, this);
        foreach (var player in CombatState.Players.Where(player => player.Creature.IsAlive).ToArray())
            await PowerCmd.Apply<RegenPower>(context, player.Creature, DynamicVars["Regen"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Regen"].UpgradeValueBy(1m);
}
