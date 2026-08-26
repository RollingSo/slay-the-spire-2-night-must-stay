using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Relics;

// The hidden controller power connects the character's summon lifecycle.
public sealed class SmallMakeupBrush : RelicModel
{
    private string _pendingNecroCategory;
    private string _pendingNecroEntry;
    private int _pendingNecroOriginalHp;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool SpawnsPets => true;
    public override string PackedIconPath => "res://revenant_assets/relics/revenant_starter_relic.png";
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
        await RevenantSummonManager.For(Owner).SummonMarkedNecro(context);
    }
}
