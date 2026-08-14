using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    public sealed class PowerfulGuardCounter : CardModel
    {
        public override bool GainsBlock => true;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };

        private const string GuardCounterKey = "GuardCounter";
        private const string BlockNextTurnKey = "BlockNextTurn";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar(GuardCounterKey, 22m),
            new BlockVar(BlockNextTurnKey, 16m, ValueProp.Move),
        };

        public PowerfulGuardCounter()
            : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            decimal guardCounter = base.DynamicVars[GuardCounterKey].BaseValue;
            BlockVar blockVar = (BlockVar)base.DynamicVars[BlockNextTurnKey];
            IEnumerable<AbstractModel> modifiers;
            decimal blockNextTurn = Hook.ModifyBlock(base.CombatState, base.Owner.Creature, blockVar.BaseValue, blockVar.Props, this, cardPlay, out modifiers);

            await PowerCmd.Apply<GuardCounterNextTurnPower>(choiceContext, base.Owner.Creature, guardCounter, base.Owner.Creature, this);
            await PowerCmd.Apply<GuardCounterBlockNextTurnPower>(choiceContext, base.Owner.Creature, blockNextTurn, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars[GuardCounterKey].UpgradeValueBy(6m);
            base.DynamicVars[BlockNextTurnKey].UpgradeValueBy(6m);
        }
    }
}
