using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace sts2mod.Core.Models.Cards
{
    public sealed class BreathingRoom : GuardianConcealedEdgeCard
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { base.EnergyHoverTip };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new EnergyVar(2)
        };

        public BreathingRoom()
            : base(3, CardRarity.Common)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
        }

        protected override void OnUpgrade() => base.DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
