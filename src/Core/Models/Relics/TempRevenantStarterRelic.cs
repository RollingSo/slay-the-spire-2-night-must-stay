using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;

namespace NightMustStay.Core.Models.Relics;

// The hidden controller power connects the character's summon lifecycle.
public abstract class RevenantSummonRelicModel : RelicModel
{
    private string _pendingNecroCategory;
    private string _pendingNecroEntry;
    private int _pendingNecroOriginalHp;

    public override bool SpawnsPets => true;
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new HoverTip(
            new LocString("cards", "REVENANT_CALL.tooltipTitle"),
            new LocString("cards", "REVENANT_CALL.tooltipDescription")),
    };

    [SavedProperty]
    public string PendingNecroCategory
    {
        get => _pendingNecroCategory;
        set
        {
            AssertMutable();
            _pendingNecroCategory = value;
        }
    }

    [SavedProperty]
    public string PendingNecroEntry
    {
        get => _pendingNecroEntry;
        set
        {
            AssertMutable();
            _pendingNecroEntry = value;
        }
    }

    [SavedProperty]
    public int PendingNecroOriginalHp
    {
        get => _pendingNecroOriginalHp;
        set
        {
            AssertMutable();
            _pendingNecroOriginalHp = value;
        }
    }

    public void MarkNecroForNextCombat(MonsterModel monster, int originalHp)
    {
        PendingNecroCategory = monster.Id.Category;
        PendingNecroEntry = monster.Id.Entry;
        PendingNecroOriginalHp = originalHp;
    }

    public bool TryGetPendingNecro(out MonsterModel monster, out int originalHp)
    {
        originalHp = PendingNecroOriginalHp;
        monster = string.IsNullOrWhiteSpace(PendingNecroCategory) ||
                  string.IsNullOrWhiteSpace(PendingNecroEntry)
            ? null
            : ModelDb.GetByIdOrNull<MonsterModel>(
                new ModelId(PendingNecroCategory, PendingNecroEntry));
        return monster != null && originalHp > 0;
    }

    public void ClearPendingNecro()
    {
        PendingNecroCategory = null;
        PendingNecroEntry = null;
        PendingNecroOriginalHp = 0;
    }

    public override async Task BeforeCombatStart()
    {
        var context = new BlockingPlayerChoiceContext();
        await PowerCmd.Apply<RevenantSummonControllerPower>(
            context, Owner.Creature, 1m, Owner.Creature, null);
        await RevenantCall.ChooseFamilyAndCall(context, Owner);
        await AfterInitialCall(context);
        await RevenantSummonManager.For(Owner).SummonMarkedNecro(context);
    }

    protected virtual Task AfterInitialCall(PlayerChoiceContext context) =>
        Task.CompletedTask;
}

public sealed class SmallMakeupBrush : RevenantSummonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string PackedIconPath => "res://revenant_assets/relics/revenant_starter_relic.png";
}

public sealed class TreasuredMakeupBrush : RevenantSummonRelicModel
{
    private bool _initialResonancePending;

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "res://revenant_assets/relics/treasured_makeup_brush.png";

    protected override Task AfterInitialCall(PlayerChoiceContext context)
    {
        Flash();
        // Defer the opening Resonance until the first turn has actually
        // started. Otherwise Helen's Retreat grants Energy before the engine
        // initializes the turn's Energy and the gain is immediately erased.
        _initialResonancePending = true;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartLate(
        PlayerChoiceContext context,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (!_initialResonancePending || player != Owner)
            return;
        _initialResonancePending = false;
        await RevenantSummonManager.For(Owner).TriggerResonance(context);
    }
}
