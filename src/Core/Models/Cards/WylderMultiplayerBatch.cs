using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;

public sealed class WylderMultiplayerCard : WylderAbilityCard<WylderMultiplayerPower>
{
    public WylderMultiplayerCard() : base(0, CardRarity.Rare) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(IsUpgraded));
    protected override void OnUpgrade() { }
}
public sealed class WylderTinder : WylderCard
{
    public WylderTinder() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(IsUpgraded));
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), c => c.IsTransformable, this)).FirstOrDefault();
        if (selected == null) return;
        var result = CombatState.CreateCard<WylderFireCoating>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(result);
        await CardCmd.Transform(selected, result);
    }
    protected override void OnUpgrade() { }
}
public sealed class WylderDefiance : WylderCard
{
    public WylderDefiance() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(19m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<SacredFeatherDrawReductionPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
public sealed class WylderBreakthrough : WylderCard
{
    public WylderBreakthrough() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VulnerablePower>("Vulnerable", 1m), new PowerVar<VigorPower>("Vigor", 4m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<VulnerablePower>(context, play.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars["Vulnerable"].UpgradeValueBy(1m); DynamicVars["Vigor"].UpgradeValueBy(1m); }
}
public sealed class WylderHoarfrostStomp : WylderFollowThroughCard
{
    public WylderHoarfrostStomp() : base(1, CardRarity.Uncommon, CardType.Skill, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<FreezePower>("Freeze", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<FreezePower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray())
            await PowerCmd.Apply<FreezePower>(context, enemy, DynamicVars["Freeze"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(1m);
}
public sealed class WylderOffensiveInstinct : WylderAbilityCard<WylderOffensiveInstinctPower>
{
    public WylderOffensiveInstinct() : base(1, CardRarity.Uncommon) { }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VulnerablePower>());
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
