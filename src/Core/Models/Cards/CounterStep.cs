using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    public sealed class CounterStep : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            base.EnergyHoverTip,
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        private const string EnergyOnSuccessKey = "EnergyOnSuccess";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new EnergyVar(EnergyOnSuccessKey, 1)
        };

        public CounterStep()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<CounterStepPower>(choiceContext, base.Owner.Creature, base.DynamicVars[EnergyOnSuccessKey].BaseValue, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars[EnergyOnSuccessKey].UpgradeValueBy(1m);
        }
    }
}
