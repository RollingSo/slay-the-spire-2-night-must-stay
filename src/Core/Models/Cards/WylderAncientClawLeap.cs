using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderAncientClawLeap : WylderCard
{
 public WylderAncientClawLeap() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.Self) { }
 public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
 protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(2) };
 protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
 {
  await WylderPursuitPower.Enter(context, Owner.Creature, this);
  await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
 }
 protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
