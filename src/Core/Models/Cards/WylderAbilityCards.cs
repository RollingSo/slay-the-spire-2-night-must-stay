using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public abstract class WylderAbilityCard<T> : WylderCard where T : WylderAbilityPower
{
    protected WylderAbilityCard(int cost, CardRarity rarity) : base(cost, CardType.Power, rarity, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        T power = await PowerCmd.Apply<T>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded) power.UpgradedCopies++;
    }
}
public sealed class WylderRuinousGhostflame : WylderAbilityCard<WylderRuinousGhostflamePower>
{
    public WylderRuinousGhostflame() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_ruinous_ghostflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Freeze", 2m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(1m);
}
public sealed class WylderVeteranEdge : WylderAbilityCard<WylderVeteranEdgePower>
{
    public WylderVeteranEdge() : base(1, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_veteran_edge.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Strength", 2m), new EnergyVar(1) };
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}


