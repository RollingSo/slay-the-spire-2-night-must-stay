using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards;

public sealed class CooperativeDefense : CardModel
{
    public override bool GainsBlock => true;
    // Approved three-shield artwork, with Guardian's wing-shaped shield centered.
    public override string PortraitPath => "res://packed/card_portraits/guardian/cooperative_defense.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1), new CalculationBaseVar(0m), new CalculationExtraVar(3m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) =>
            PileType.Hand.GetPile(card.Owner).Cards.Count(other => other != card && GuardianCardFilters.HasDefendInName(other)))
    };
    public CooperativeDefense() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, cardPlay);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
