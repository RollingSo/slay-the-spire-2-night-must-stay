using System.Collections.Generic;
using System.Threading.Tasks;
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
    public sealed class AllOutCounter : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };

        private const string GuardCounterKey = "GuardCounter";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(6m, ValueProp.Move),
            new DynamicVar(GuardCounterKey, 14m)
        };

        public AllOutCounter()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            System.ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            await PowerCmd.Apply<GuardCounterPower>(choiceContext, base.Owner.Creature, base.DynamicVars[GuardCounterKey].BaseValue, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
            base.DynamicVars[GuardCounterKey].UpgradeValueBy(4m);
        }
    }
}
