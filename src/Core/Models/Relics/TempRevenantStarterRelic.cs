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
    private bool _initialCallPending;

    public override bool SpawnsPets => true;
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override bool ShouldFlashOnPlayer => false;

    [SavedProperty]
    public bool InitialCallPending
    {
        get => _initialCallPending;
        set
        {
            AssertMutable();
            _initialCallPending = value;
        }
    }

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
        InitialCallPending = true;
        await PowerCmd.Apply<RevenantSummonControllerPower>(
            new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
    }

    public async Task PerformInitialCall(PlayerChoiceContext context)
    {
        if (!InitialCallPending)
            return;

        // This is deliberately resolved from the owner's turn-start action
        // queue. In multiplayer each Revenant then receives an independent
        // choice action, so several opening Call screens can coexist.
        InitialCallPending = false;
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
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => "res://revenant_assets/relics/treasured_makeup_brush.png";

    protected override async Task AfterInitialCall(PlayerChoiceContext context)
    {
        Flash();
        // Initial Call now runs after the turn's Energy initialization, so
        // Helen's opening Retreat gain is preserved without a deferred flag.
        await RevenantSummonManager.For(Owner).TriggerResonance(context);
    }
}
