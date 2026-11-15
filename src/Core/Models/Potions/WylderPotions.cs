using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.CardPools;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Potions;

public sealed class WylderFireGrease : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<VigorPower>(8m) };
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<VigorPower>() };

    protected override Task OnUse(PlayerChoiceContext context, Creature target) =>
        PowerCmd.Apply<VigorPower>(context, Owner.Creature,
            DynamicVars[nameof(VigorPower)].BaseValue, Owner.Creature, null);
}

public sealed class WylderBraveMeat : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<StrengthPower>(3m) };
    public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<WylderPursuitPower>()
    };

    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        await PowerCmd.Apply<StrengthPower>(context, Owner.Creature,
            DynamicVars[nameof(StrengthPower)].BaseValue, Owner.Creature, null);
        await WylderPursuitPower.Enter(context, Owner.Creature, null,
            WylderEffectSource.PotionEffect);
    }
}

public sealed class WylderStoneSwordKey : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(2) };

    protected override async Task OnUse(PlayerChoiceContext context, Creature target)
    {
        WylderDesperationCard[] choices = ModelDb.CardPool<WylderCardPool>()
            .AllCards.OfType<WylderDesperationCard>().ToArray();
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            CardModel card = Owner.Creature.CombatState.CreateCard(
                Owner.RunState.Rng.Niche.NextItem(choices), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }
}
