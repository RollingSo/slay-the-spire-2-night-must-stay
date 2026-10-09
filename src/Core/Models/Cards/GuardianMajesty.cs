using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models;

namespace NightMustStay.Core.Models.Cards;

public sealed class GuardianMajesty : CardModel
{
    public GuardianMajesty() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/guardian_majesty.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedStrengthLoss").WithMultiplier(static (card, _) =>
            card.Owner?.PlayerCombatState == null ? 0 :
                PileType.Hand.GetPile(card.Owner).Cards.Count(GuardianCardFilters.HasDefendInName))
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<StrengthPower>() };

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        decimal loss = ((CalculatedVar)DynamicVars["CalculatedStrengthLoss"]).Calculate(null);
        if (loss <= 0) return;
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray())
            await PowerCmd.Apply<StrengthPower>(context, enemy, -loss, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
