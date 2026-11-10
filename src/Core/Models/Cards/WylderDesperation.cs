using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderDesperationCard : WylderCard
{
    protected WylderDesperationCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target) { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(
        new HoverTip(new LocString("cards", "WYLDER_DESPERATION.title"),
            new LocString("cards", "WYLDER_DESPERATION.description")));

    protected virtual IEnumerable<Creature> LegalDesperationTargets() =>
        CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive);

    internal IEnumerable<Creature> GrantedLegalTargets() => LegalDesperationTargets();
    public override Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
        => WylderGrantedDesperation.Run(this, context, side, participants);
}
