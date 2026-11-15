using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderCriticalStrike : WylderStunReturningAttack
{
    public WylderCriticalStrike() : base(3, CardRarity.Uncommon) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(16m, ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
        { HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
