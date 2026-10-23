using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Relics;

public abstract class RevenantRelicModel : RelicModel
{
    protected abstract string RevenantIconName { get; }

    public override string PackedIconPath =>
        $"res://revenant_assets/relics/{RevenantIconName}.png";
    protected override string PackedIconOutlinePath => PackedIconPath.Replace(".png", "_outline.png");
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;
}

public sealed class DirtyPhotoFrame : RevenantRelicModel
{
    protected override string RevenantIconName => "dirty_photo_frame";
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeHandDraw(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        PlayerChoiceContext context,
        ICombatState combatState)
    {
        // Like native Toolbox, gather the opening choice inside this player's
        // turn-start action, not the globally awaited combat initialization.
        // Its context pauses only this owner while teammates keep playing.
        if (player != Owner || Owner.PlayerCombatState.TurnNumber != 1)
            return;

        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0)
            return;

        var selected = (await CardSelectCmd.FromCombatPile(
            context,
            draw,
            Owner,
            new CardSelectorPrefs(
                new LocString("relics", "DIRTY_PHOTO_FRAME.selectionScreenPrompt"),
                Math.Min(3, draw.Cards.Count)))).ToList();
        if (selected.Count == 0)
            return;

        Flash();
        foreach (CardModel card in selected)
            if (card.Pile == draw) await CardPileCmd.Add(card, PileType.Discard);
    }
}

public sealed class MiniatureMakeupTools : RevenantRelicModel
{
    protected override string RevenantIconName => "miniature_makeup_tools";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<StrengthPower>(1m) };

    public async Task AfterFamilyCalled(PlayerChoiceContext context)
    {
        Creature family = RevenantSummonManager.For(Owner).CurrentFamilyCreature;
        if (family is not { IsAlive: true })
            return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            context,
            family,
            DynamicVars.Strength.BaseValue,
            Owner.Creature,
            null);
    }
}

public sealed class DeepSeaNight : RevenantRelicModel
{
    protected override string RevenantIconName => "deep_sea_night";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("MaxHp", 2m) };

    public async Task AfterResonance()
    {
        if (!RevenantSummonManager.For(Owner).HasLivingFamily)
            return;
        Flash();
        await RevenantSummonManager.For(Owner)
            .IncreaseFamilyMaxHp(DynamicVars["MaxHp"].BaseValue);
    }
}

public sealed class OldPocketPortrait : RevenantRelicModel
{
    protected override string RevenantIconName => "old_pocket_portrait";
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new CardsVar(1) };

    public async Task AfterResonance(PlayerChoiceContext context)
    {
        Flash();
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }
}

public sealed class BlueAmberAmulet : RevenantRelicModel
{
    private const int RecoverThreshold = 3;
    private int _recoverProgress;

    protected override string RevenantIconName => "blue_amber_amulet";
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => RecoverProgress;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Recover", RecoverThreshold),
        new EnergyVar(1),
    };

    [SavedProperty]
    public int RecoverProgress
    {
        get => _recoverProgress;
        set
        {
            AssertMutable();
            _recoverProgress = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel source)
    {
        if (card.Owner != Owner || !RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            return;

        RecoverProgress++;
        while (RecoverProgress >= RecoverThreshold)
        {
            RecoverProgress -= RecoverThreshold;
            Flash();
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RecoverProgress = 0;
        return Task.CompletedTask;
    }
}

public sealed class BelieversVowCloth : RevenantRelicModel
{
    private sealed class PendingChargeDiscount { }
    private static readonly ConditionalWeakTable<CardModel, PendingChargeDiscount> PendingDiscounts = new();

    protected override string RevenantIconName => "believers_vow_cloth";
    public override RelicRarity Rarity => RelicRarity.Rare;

    public void AfterChargeCompleted(CardModel card)
    {
        if (card?.Owner != Owner)
            return;
        Flash();
        card.EnergyCost.SetUntilPlayed(0);
        // Self-charging happens inside this card's OnPlay. Its native cleanup
        // must not consume the discount just earned for the NEXT play.
        // Harmony/Preparation Ritual charge a different card in hand, so their
        // discounts need no restoration after that card is actually played.
        if (card.Pile?.Type == PileType.Play)
            PendingDiscounts.GetOrCreateValue(card);
    }

    internal static bool RestoreChargeDiscountAfterPlayCleanup(CardModel card)
    {
        if (card == null || !PendingDiscounts.Remove(card)
            || card is not IRevenantChargeCard { IsChargeComplete: true }
            || card.Owner?.GetRelic<BelieversVowCloth>() == null)
            return false;

        card.EnergyCost.SetUntilPlayed(0);
        return true;
    }
}

public sealed class PortableSewingKit : RevenantRelicModel
{
    protected override string RevenantIconName => "portable_sewing_kit";
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("MaxHp", 1m) };

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel source)
    {
        if (card.Owner != Owner || !RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            return;
        if (!RevenantSummonManager.For(Owner).HasLivingFamily)
            return;
        Flash();
        await RevenantSummonManager.For(Owner)
            .IncreaseFamilyMaxHp(DynamicVars["MaxHp"].BaseValue);
    }
}
