using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class ShieldedThrust : CardModel
    {
        private const string FortifyKey = "Fortify";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new DynamicVar(FortifyKey, 3m),
        };

        public ShieldedThrust()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            await PowerCmd.Apply<FortifyPower>(choiceContext, base.Owner.Creature, base.DynamicVars[FortifyKey].BaseValue, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
            base.DynamicVars[FortifyKey].UpgradeValueBy(2m);
        }
    }
}
