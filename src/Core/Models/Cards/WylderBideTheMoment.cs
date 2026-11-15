using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderBideTheMoment : WylderCard
{
    public WylderBideTheMoment() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;
    protected override bool HasPursuitCondition => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new BlockVar(8m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (!Pursuing) return;
        WylderAnimationPatch.PlayBlock(Owner.Creature);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
