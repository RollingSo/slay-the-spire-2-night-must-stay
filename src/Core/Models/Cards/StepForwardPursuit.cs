using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class StepForwardPursuit : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            base.EnergyHoverTip,
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(12m, ValueProp.Move)
        };

        public StepForwardPursuit()
            : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(6m);
        }
    }
}
