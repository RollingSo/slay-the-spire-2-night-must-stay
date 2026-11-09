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
