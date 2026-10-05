using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
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
    public override TargetType TargetType => TargetType.AnyEnemy;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(30m, ValueProp.Unpowered) };
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { ModelDb.Potion<SpiritCallingJar>().HoverTip, GuardianCardHoverTips.RevenantNecro };

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        // Capture identity before death hooks remove or replace the combat monster.
        ModelId monsterId = target.Monster?.Id;
        IEnumerable<DamageResult> results = await Core.Compatibility.Sts2BranchCompat.Damage(
            choiceContext, target, DynamicVars.Damage.BaseValue, DynamicVars.Damage.Props, Owner.Creature, null);
        if (monsterId != null && IsKillingBlow(target, results))
        {
            var jar = (SpiritCallingJar)ModelDb.Potion<SpiritCallingJar>().ToMutable();
            jar.SetCapturedMonster(monsterId);
            // The native wrapper removes this Wraith Jar before OnUse, freeing its belt slot.
            await PotionCmd.TryToProcure(jar, Owner);
        }
    }

    internal static bool IsKillingBlow(Creature target, IEnumerable<DamageResult> results) =>
        target.IsDead && results.Any(result => result.Receiver == target && result.WasTargetKilled);
}

public sealed class SpiritCallingJar : PotionModel
{
    public string CapturedMonsterCategory { get; private set; } = "";
    public string CapturedMonsterEntry { get; private set; } = "";
    public override PotionRarity Rarity => PotionRarity.Token;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    public override bool CanBeGeneratedInCombat => false;
    public override bool PassesCustomUsabilityCheck => IsMutable && TryGetCapturedMonster(out _);
    public override IEnumerable<IHoverTip> ExtraHoverTips => new[] { GuardianCardHoverTips.RevenantNecro };

    public void SetCapturedMonster(ModelId id)
    {
        AssertMutable();
        CapturedMonsterCategory = id.Category;
        CapturedMonsterEntry = id.Entry;
    }

    public bool TryGetCapturedMonster(out MonsterModel monster)
    {
        monster = null;
        if (string.IsNullOrWhiteSpace(CapturedMonsterCategory) || string.IsNullOrWhiteSpace(CapturedMonsterEntry))
            return false;
        if (CapturedMonsterCategory != ModelId.SlugifyCategory<MonsterModel>()) return false;
        // Unknown or wrong-category IDs are safe failures, not random replacement summons.
        monster = ModelDb.GetByIdOrNull<AbstractModel>(
            new ModelId(CapturedMonsterCategory, CapturedMonsterEntry)) as MonsterModel;
        return monster != null;
    }

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature target) =>
        TryGetCapturedMonster(out MonsterModel monster)
            ? RevenantSummonManager.For(Owner).SummonCapturedNecro(choiceContext, monster)
            : Task.CompletedTask;
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
