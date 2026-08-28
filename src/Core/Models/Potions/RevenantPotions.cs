using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Potions;

public sealed class DustyNote : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature target) =>
        RevenantCall.ChooseFamilyAndCall(choiceContext, Owner);
}

public sealed class WraithJar : PotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature target) =>
        RevenantSummonManager.For(Owner).ReviveRandomNecro(choiceContext);
}

public sealed class StarlightShard : PotionModel
{
    private const int RecoverCount = 3;

    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        CardPile discard = PileType.Discard.GetPile(Owner);
        int count = Math.Min(RecoverCount, discard.Cards.Count);
        if (count <= 0)
            return;

        CardModel[] selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            discard,
            Owner,
            new CardSelectorPrefs(
                new LocString("potions", "STARLIGHT_SHARD.selectionScreenPrompt"),
                count))).ToArray();
        foreach (CardModel card in selected)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}
