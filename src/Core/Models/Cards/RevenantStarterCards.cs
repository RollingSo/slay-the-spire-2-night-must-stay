using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Revenant;
using sts2mod.Core.Models.CardPools;

namespace sts2mod.Core.Models.Cards;

public sealed class StrikeRevenant : CardModel
{
    public override string PortraitPath =>
        "res://revenant_assets/cards/strike_revenant.png";
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public StrikeRevenant() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class DefendRevenant : CardModel
{
    public override string PortraitPath =>
        "res://revenant_assets/cards/defend_revenant.png";
    public override bool GainsBlock => true;
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
    };

    public DefendRevenant() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class RevenantCall : CardModel
{
    public override string PortraitPath =>
        "res://revenant_assets/cards/call.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            LocString description = new LocString("cards", "REVENANT_CALL.tooltipDescription");
            return new IHoverTip[]
            {
                new HoverTip(new LocString("cards", "REVENANT_CALL.tooltipTitle"), description),
            };
        }
    }

    // Starter-only: keep it in RevenantCardPool for its visuals/library entry,
    // while Basic rarity excludes it from ordinary card rewards.
    public RevenantCall() : base(2, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await ChooseFamilyAndCall(context, Owner);
    }

    public static async Task ChooseFamilyAndCall(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(owner);
        IReadOnlyList<RevenantFamilyId> available = manager.GetCallableFamilies();
        List<CardModel> options = available.Select(family => CreateChoiceCard(owner, family)).ToList();
        if (options.Count == 0)
            return;

        CardModel selected = await CardSelectCmd.FromChooseACardScreen(context, options, owner);
        if (selected is IRevenantFamilyChoice choice)
            await manager.CallFamily(context, choice.FamilyId);
    }

    private static CardModel CreateChoiceCard(MegaCrit.Sts2.Core.Entities.Players.Player owner, RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => owner.Creature.CombatState.CreateCard<RevenantFamilyHelenChoice>(owner),
        RevenantFamilyId.PumpkinHead => owner.Creature.CombatState.CreateCard<RevenantFamilyPumpkinHeadChoice>(owner),
        _ => owner.Creature.CombatState.CreateCard<RevenantFamilySkeletonChoice>(owner),
    };

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class RevenantResonance : CardModel
{
    public override string PortraitPath =>
        "res://revenant_assets/cards/resonance.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            LocString genericDescription = new LocString("cards", "REVENANT_RESONANCE.tooltipDescription");
            return new IHoverTip[]
            {
                new HoverTip(new LocString("cards", "REVENANT_RESONANCE.tooltipTitle"), genericDescription),
            };
        }
    }

    public RevenantResonance() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        RevenantSummonManager.For(Owner).TriggerResonance(context);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);

}

public interface IRevenantFamilyChoice
{
    RevenantFamilyId FamilyId { get; }
}

public abstract class RevenantFamilyChoiceCard : CardModel, IRevenantFamilyChoice
{
    public abstract RevenantFamilyId FamilyId { get; }
    public override CardPoolModel Pool => ModelDb.CardPool<TokenCardPool>();
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<RevenantCardPool>();

    protected RevenantFamilyChoiceCard() : base(-2, CardType.Skill, CardRarity.Token, TargetType.None) { }
}

public sealed class RevenantFamilyHelenChoice : RevenantFamilyChoiceCard
{
    public override RevenantFamilyId FamilyId => RevenantFamilyId.Helen;
    public override string PortraitPath => "res://revenant_assets/cards/helen_family.png";
}

public sealed class RevenantFamilyPumpkinHeadChoice : RevenantFamilyChoiceCard
{
    public override RevenantFamilyId FamilyId => RevenantFamilyId.PumpkinHead;
    public override string PortraitPath => "res://revenant_assets/cards/pumpkin_head_family.png";
}

public sealed class RevenantFamilySkeletonChoice : RevenantFamilyChoiceCard
{
    public override RevenantFamilyId FamilyId => RevenantFamilyId.Skeleton;
    public override string PortraitPath => "res://revenant_assets/cards/skeleton_family.png";
}
