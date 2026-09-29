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

public sealed class DuchessSmokeBottle : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override Task OnUse(PlayerChoiceContext context, Creature target) =>
        PowerCmd.Apply<DuchessConcealmentPower>(context, Owner.Creature, 3m, Owner.Creature, null);
}

public sealed class DuchessRadiantBladeCrystal : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        for (int i = 0; i < 2; i++)
        {
            DuchessRadiantBlade blade = Owner.Creature.CombatState.CreateCard<DuchessRadiantBlade>(Owner);
            CardCmd.Upgrade(blade);
            if (Owner.Creature.GetPower<DuchessRadiantBladeGrowthPower>() is { } growth)
                blade.DynamicVars.Damage.BaseValue += growth.TotalGrowth;
            await CardPileCmd.AddGeneratedCardToCombat(blade, PileType.Hand, Owner, CardPilePosition.Top);
        }
    }
}

public sealed class DuchessRegretPotion : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        await PlayerCmd.GainEnergy(2m, Owner);
        await CardPileCmd.Draw(context, 2m, Owner);
        await DuchessMomentPower.Set(context, Owner.Creature, 0, this);
    }
}
