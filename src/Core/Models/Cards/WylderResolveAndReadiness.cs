using System.Linq;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderResolve : WylderCard
{
    public WylderResolve() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(3) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<SacredFeatherDrawReductionPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
public sealed class WylderRouse : WylderCard
{
    public WylderRouse() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new EnergyVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<SacredFeatherDrawReductionPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public sealed class WylderInspire : WylderCard
{
    public WylderInspire() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_inspire.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new CardsVar(2), new DynamicVar("HandThreshold", 2m) };
    protected override bool ShouldGlowGoldInternal =>
        base.ShouldGlowGoldInternal || WillHaveLowHandAfterPlay;

    private bool WillHaveLowHandAfterPlay => IsMutable
        && Owner != null
        && Pile?.Type == PileType.Hand
        && PileType.Hand.GetPile(Owner).Cards.Count - 1 <= DynamicVars["HandThreshold"].IntValue;

    private bool HasLowHandAfterPlay => IsMutable
        && Owner != null
        && PileType.Hand.GetPile(Owner).Cards.Count <= DynamicVars["HandThreshold"].IntValue;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await WylderPursuitPower.Enter(context, Owner.Creature, this);
        if (HasLowHandAfterPlay)
            await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public sealed class WylderBattleReadiness : WylderCard
{
 public WylderBattleReadiness() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
 public override bool GainsBlock => true;
 protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(8m, ValueProp.Move) };
 protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
 {
  await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
  var attack = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault(c => c.Type == CardType.Attack);
  if (attack == null) return;
  await CardPileCmd.Add(attack, PileType.Draw, CardPilePosition.Top, this);
  await CardPileCmd.Draw(context, 1m, Owner);
 }
 protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
