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

public sealed class WylderValor : WylderCard
{
    public WylderValor() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new BlockVar(7m, ValueProp.Move), new PowerVar<StrengthPower>("Strength", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(HoverTipFactory.FromPower<StrengthPower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
        await PowerCmd.Apply<WylderValorStrengthPower>(context, Owner.Creature,
            DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Strength"].UpgradeValueBy(1m);
    }
}

public sealed class WylderValorStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<WylderValor>();
}
