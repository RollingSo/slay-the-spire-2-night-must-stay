using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderSpark : WylderCard
{
    public WylderSpark() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VigorPower>("Vigor", 3m), new CardsVar(1) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this);
        await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(2m);
}

public sealed class WylderRollingAttack : WylderCard
{
    public WylderRollingAttack() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new CardsVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        bool pursuit = Pursuing;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(play.Target).Execute(context);
        if (pursuit) await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
