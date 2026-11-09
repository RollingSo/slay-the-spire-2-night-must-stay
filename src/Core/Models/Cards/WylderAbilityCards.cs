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
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<T>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        T power = await PowerCmd.Apply<T>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && IsUpgraded) power.UpgradedCopies++;
    }
}

public sealed class WylderEmberReserve : WylderAbilityCard<WylderEmberReservePower>
{
    public WylderEmberReserve() : base(1, CardRarity.Common) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 4m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}

public sealed class WylderClashEmbers : WylderAbilityCard<WylderClashEmbersPower>
{
    public WylderClashEmbers() : base(1, CardRarity.Common) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Vigor", 1m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(1m);
}

public sealed class WylderOpeningHunt : WylderAbilityCard<WylderOpeningHuntPower>
{
    public WylderOpeningHunt() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/dark_embrace.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Vigor", 0m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(2m);
}

public sealed class WylderWindwardStance : WylderAbilityCard<WylderWindwardStancePower>
{
    public WylderWindwardStance() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 4m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}

public sealed class WylderBattleInstinct : WylderAbilityCard<WylderBattleInstinctPower>
{
    public WylderBattleInstinct() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/dark_embrace.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Reduction", 1m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(1m);
}

public sealed class WylderBloodStillHot : WylderAbilityCard<WylderBloodStillHotPower>
{
    public WylderBloodStillHot() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 5m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}

public sealed class WylderFlameGuardStance : WylderAbilityCard<WylderFlameGuardStancePower>
{
    public WylderFlameGuardStance() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 0m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(3m);
}

public sealed class WylderPursuitCommand : WylderAbilityCard<WylderPursuitCommandPower>
{
    public WylderPursuitCommand() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/dark_embrace.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Cards", 1m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class WylderBattlefieldMight : WylderAbilityCard<WylderBattlefieldMightPower>
{
    public WylderBattlefieldMight() : base(2, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Strength", 1m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}

public sealed class WylderGuardResurgence : WylderAbilityCard<WylderGuardResurgencePower>
{
    public WylderGuardResurgence() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 4m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(3m);
}

public sealed class WylderSwiftBattleWill : WylderAbilityCard<WylderSwiftBattleWillPower>
{
    public WylderSwiftBattleWill() : base(1, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/dark_embrace.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 4m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2m);
}

public sealed class WylderRuinousGhostflame : WylderAbilityCard<WylderRuinousGhostflamePower>
{
    public WylderRuinousGhostflame() : base(1, CardRarity.Uncommon) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_ruinous_ghostflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Freeze", 2m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Freeze"].UpgradeValueBy(1m);
}

public sealed class WylderPursuitStance : WylderAbilityCard<WylderPursuitStancePower>
{
    public WylderPursuitStance() : base(2, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 8m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(4m);
}

public sealed class WylderFieryBlood : WylderAbilityCard<WylderFieryBloodPower>
{
    public WylderFieryBlood() : base(1, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Vigor", 2m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(1m);
}

public sealed class WylderVeteranEdge : WylderAbilityCard<WylderVeteranEdgePower>
{
    public WylderVeteranEdge() : base(1, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_veteran_edge.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Strength", 2m), new EnergyVar(1) };
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

public sealed class WylderUnyieldingSpirit : WylderAbilityCard<WylderUnyieldingSpiritPower>
{
    public WylderUnyieldingSpirit() : base(1, CardRarity.Rare) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Block", 8m), new EnergyVar(1) };
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(4m);
}

public sealed class WylderNightEdge : WylderAbilityCard<WylderNightEdgePower>
{
    public WylderNightEdge() : base(2, CardRarity.Ancient) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Reduction", 5m), new EnergyVar(1), new DynamicVar("Vigor", 3m) };
    protected override void OnUpgrade() { DynamicVars["Reduction"].UpgradeValueBy(2m); DynamicVars["Vigor"].UpgradeValueBy(1m); }
}


