using System;
using System.Collections.Generic;
using System.Linq;
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
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;
}

public sealed class DirtyPhotoFrame : RevenantRelicModel
{
    protected override string RevenantIconName => "dirty_photo_frame";
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0)
            return;

        var context = new BlockingPlayerChoiceContext();
        CardModel selected = (await CardSelectCmd.FromCombatPile(
            context,
            draw,
            Owner,
            new CardSelectorPrefs(
                new LocString("relics", "DIRTY_PHOTO_FRAME.selectionScreenPrompt"),
                1))).FirstOrDefault();
        if (selected == null)
            return;

        Flash();
        await CardPileCmd.Add(selected, PileType.Discard);
    }
}

public sealed class MiniatureMakeupTools : RevenantRelicModel
{
    protected override string RevenantIconName => "miniature_makeup_tools";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new PowerVar<StrengthPower>(2m) };

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
    private bool _triggeredThisTurn;

    protected override string RevenantIconName => "old_pocket_portrait";
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new CardsVar(1) };

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
            _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public async Task AfterResonance(PlayerChoiceContext context)
    {
        if (_triggeredThisTurn)
            return;
        _triggeredThisTurn = true;
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
    protected override string RevenantIconName => "believers_vow_cloth";
    public override RelicRarity Rarity => RelicRarity.Rare;

    public void AfterChargeCompleted(CardModel card)
    {
        if (card?.Owner != Owner)
            return;
        Flash();
        card.EnergyCost.SetUntilPlayed(0);
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
