using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Potions;

public sealed class DuchessSmokeBottle : PotionModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<DuchessConcealmentPower>() };
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override Task OnUse(PlayerChoiceContext context, Creature target)
    {
        AssertValidForTargetedPotion(target);
        return PowerCmd.Apply<DuchessConcealmentPower>(context, target, 3m, Owner.Creature, null);
    }
}

public sealed class DuchessRadiantBladeCrystal : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        AssertValidForTargetedPotion(target);
        for (int i = 0; i < 2; i++)
        {
            DuchessRadiantBlade blade = target.CombatState.CreateCard<DuchessRadiantBlade>(target.Player);
            CardCmd.Upgrade(blade);
            if (target.GetPower<DuchessRadiantBladeGrowthPower>() is { } growth)
                blade.DynamicVars.Damage.BaseValue += growth.TotalGrowth;
            await CardPileCmd.AddGeneratedCardToCombat(blade, PileType.Hand, Owner, CardPilePosition.Top);
        }
    }
}

public sealed class DuchessRegretPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PlayerCmd.GainEnergy(2m, target.Player);
        await CardPileCmd.Draw(context, 2m, target.Player);
        await DuchessMomentPower.Set(context, target, 0, this);
    }
}
