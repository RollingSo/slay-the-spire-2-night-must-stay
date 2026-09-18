using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Relics;

public abstract class DuchessRelic : RelicModel
{
    public override string PackedIconPath => $"res://duchess_assets/relics/{Id.Entry.ToLowerInvariant()}.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
}

public class DuchessOldPocketwatch : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected virtual decimal CardsToDraw => 1m;

    public async Task OnMomentFive(PlayerChoiceContext context)
    {
        Flash();
        await CardPileCmd.Draw(context, CardsToDraw, Owner);
    }
}

public sealed class DuchessMendedPocketwatch : DuchessOldPocketwatch
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override decimal CardsToDraw => 2m;
}

public sealed class DuchessLaceCuff : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task BeforeCombatStart()
    {
        DuchessDodge dodge = Owner.Creature.CombatState.CreateCard<DuchessDodge>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dodge, PileType.Draw, Owner, CardPilePosition.Random);
    }
}

public sealed class DuchessSilverThimble : DuchessRelic
{
    private int _lastTurnTriggered = -1;
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card.Owner != Owner || card is not DuchessCard { HasReaction: true }
            || oldPileType != PileType.Draw || card.Pile?.Type != PileType.Hand
            || Owner.PlayerCombatState.Phase != PlayerTurnPhase.Play
            || _lastTurnTriggered == Owner.PlayerCombatState.TurnNumber)
            return;
        _lastTurnTriggered = Owner.PlayerCombatState.TurnNumber;
        Flash();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1m, Owner);
    }
}

public sealed class DuchessDanceShoes : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        Flash();
        await DuchessMomentPower.Advance(context, Owner.Creature, 1, this);
    }
}

public sealed class DuchessUnsentLetter : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner && player.PlayerCombatState.TurnNumber == 1)
            await CardPileCmd.Draw(context, 2m, Owner);
    }
}

public sealed class DuchessBlueRibbon : DuchessRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner && player.PlayerCombatState.TurnNumber == 1)
            await CreatureCmd.GainBlock(Owner.Creature, 6m, ValueProp.Unpowered, null);
    }
}
