using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class IroneyeReadiness : CardModel
{
    public IroneyeReadiness() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironeye/ironeye_readiness.png";
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Retreat>(IsUpgraded);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).FirstOrDefault();
        if (selected == null) return;
        var replacement = CombatState.CreateCard<Retreat>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(replacement);
        await CardCmd.Transform(selected, replacement);
    }
    protected override void OnUpgrade() { }
}

public sealed class StrangleCommand : CardModel
{
    public StrangleCommand() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironeye/strangle_command.png";
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<DistancePower>(), HoverTipFactory.FromPower<StrengthPower>()
    };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        await PowerCmd.Apply<StrangleCommandPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
