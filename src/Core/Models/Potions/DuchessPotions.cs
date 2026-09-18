using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Potions;

public sealed class DuchessSilverPerfume : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override Task OnUse(PlayerChoiceContext context, Creature target) =>
        DuchessMomentPower.Advance(context, Owner.Creature, 3, this);
}

public sealed class DuchessVeilVial : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        for (int i = 0; i < 2; i++)
        {
            DuchessDodge dodge = Owner.Creature.CombatState.CreateCard<DuchessDodge>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(dodge, PileType.Hand, Owner, CardPilePosition.Top);
        }
    }
}

public sealed class DuchessMemoryDraught : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        await DuchessMomentPower.Set(context, Owner.Creature, 5, this);
        await CardPileCmd.Draw(context, 1m, Owner);
    }
}
