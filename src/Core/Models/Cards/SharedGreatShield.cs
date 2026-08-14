using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    public sealed class SharedGreatShield : CardModel
    {
        private const string FortifyKey = "Fortify";

        public override bool GainsBlock => true;

        protected override System.Collections.Generic.IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<FortifyPower>()
        };

        protected override System.Collections.Generic.IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(16m, ValueProp.Move),
            new DynamicVar(FortifyKey, 6m)
        };

        public SharedGreatShield()
            : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<NoAttacksNextTurnPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<FortifyPower>(choiceContext, base.Owner.Creature, base.DynamicVars[FortifyKey].BaseValue, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Block.UpgradeValueBy(4m);
            base.DynamicVars[FortifyKey].UpgradeValueBy(2m);
        }
    }
}
