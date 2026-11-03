using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

// Final horizontal-review proposals from the designated planning chat.
// Verified native portraits remain interim assets, never approved custom art.
public sealed class WylderVerticalPressure : WylderCard
{
    public WylderVerticalPressure() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/tear_asunder.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(18m, ValueProp.Move), new DynamicVar("Break", 30m), new CardsVar(2) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (BrokeThisPlay) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}

public sealed class WylderGhostflameCinders : WylderCard
{
    public WylderGhostflameCinders() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/defect/cold_snap.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new PowerVar<FreezePower>("Freeze", 1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<FreezePower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (play.Target?.IsAlive == true)
            await PowerCmd.Apply<FreezePower>(context, play.Target, DynamicVars["Freeze"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars["Freeze"].UpgradeValueBy(1m); }
}

public sealed class WylderFleetFootwork : WylderCard
{
    public WylderFleetFootwork() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/silent/acrobatics.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(4m, ValueProp.Move), new CardsVar(1) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        if (attacked) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class WylderOffensiveShift : WylderCard
{
    public WylderOffensiveShift() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(5m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (pursuit) await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
        else await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderLastStandGuard : WylderCard
{
    public WylderLastStandGuard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(15m, ValueProp.Move), new DynamicVar("HandThreshold", 3m), new DynamicVar("CostReduction", 1m) };
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || !WylderHandPlayContext.IsLowHand(this, null, DynamicVars["HandThreshold"].IntValue)) return false;
        modifiedCost = Math.Max(0m, originalCost - DynamicVars["CostReduction"].BaseValue);
        return true;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}

public sealed class WylderPatientFocus : WylderCard
{
    public WylderPatientFocus() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(5m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        if (attacked) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        else await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); DynamicVars["Vigor"].UpgradeValueBy(1m); }
}

public sealed class WylderTwinEdgeCombo : WylderCard
{
    public WylderTwinEdgeCombo() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/uppercut.png";
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new RepeatVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue).Execute(context);
        if (pursuit) await WylderPursuitPower.Enter(context, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}

public sealed class WylderAssaultStake : WylderDesperationCard
{
    public WylderAssaultStake() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(28m, ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
        { HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray())
            await CreatureCmd.Stun(enemy);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class WylderBraveBlessing : WylderCard
{
    public WylderBraveBlessing() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(8m, ValueProp.Move), new PowerVar<VigorPower>("Vigor", 3m), new DynamicVar("VigorThreshold", 5m), new CardsVar(2) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (Owner.Creature.GetPowerAmount<VigorPower>() >= DynamicVars["VigorThreshold"].BaseValue)
            await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        else await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class WylderFinalGuard : WylderCard
{
    public WylderFinalGuard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/shrug_it_off.png";
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(16m, ValueProp.Move), new EnergyVar(1), new DynamicVar("HandCount", 2m) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool exactlyTwo = SourceHandCount == DynamicVars["HandCount"].IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        if (exactlyTwo) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public sealed class WylderWildBreath : WylderCard
{
    public WylderWildBreath() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VigorPower>("Vigor", 4m), new CardsVar(2) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<VigorPower>() });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool attacked = WylderPlayHistory.HasPrevious(this, play, CardType.Attack);
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
        if (attacked) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(2m);
}
