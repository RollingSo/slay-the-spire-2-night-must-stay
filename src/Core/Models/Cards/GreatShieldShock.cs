using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class GreatShieldShock : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            // The power deals unpowered damage, not an attack. Its preview
            // must use the same flags so Strength/Weak cannot modify it.
            new DamageVar(4m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered | MegaCrit.Sts2.Core.ValueProps.ValueProp.SkipHurtAnim)
        };

        public GreatShieldShock()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<GreatShieldShockPower>(choiceContext, base.Owner.Creature, base.DynamicVars.Damage.BaseValue, base.Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
        }
    }
}
