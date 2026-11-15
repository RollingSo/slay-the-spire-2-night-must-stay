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
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderSeamless : WylderAbilityCard<WylderSeamlessPower>
{
    public const int BaseBlock = 10;
    public const int UpgradedBlock = 14;

    public WylderSeamless() : base(1, CardRarity.Rare) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new BlockVar(BaseBlock, ValueProp.Move) };
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
}

public sealed class WylderMoonlightSword : WylderAbilityCard<WylderMoonlightSwordPower>
{
    public const int BaseFreeze = 2;
    public const int UpgradedFreeze = 3;

    public WylderMoonlightSword() : base(1, CardRarity.Rare) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new PowerVar<FreezePower>("Freeze", BaseFreeze) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(HoverTipFactory.FromPower<FreezePower>());
    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(UpgradedFreeze - BaseFreeze);
}

public sealed class WylderTwinToken : WylderCard
{
    public WylderTwinToken() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderSixthSense>(false));

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var player = play.Target?.Player;
        if (player == null || player.Creature.IsDead) return;

        var sixthSense = player.RunState.CreateCard<WylderSixthSense>(player);
        var result = await CardPileCmd.Add(sixthSense, PileType.Deck, CardPilePosition.Random, this);
        if (!result.success) return;

        if (DeckVersion is { } deckVersion && deckVersion.Pile?.Type == PileType.Deck)
            await CardPileCmd.RemoveFromDeck(deckVersion, false);
        await CardPileCmd.RemoveFromCombat(this, false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
