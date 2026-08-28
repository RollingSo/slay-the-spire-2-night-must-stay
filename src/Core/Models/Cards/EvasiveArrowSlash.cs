using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class EvasiveArrowSlash : CardModel
    {
        private const string DamageKey = "Damage";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new[] { new PowerVar<EvasiveArrowSlashPower>(DamageKey, 2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<DistancePower>(),
                HoverTipFactory.FromPower<EvasiveArrowSlashPower>(),
            };

        public override string PortraitPath =>
            ImageHelper.GetImagePath(
                "packed/card_portraits/ironeye/evasive_arrow_slash.png");

        public EvasiveArrowSlash()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            await PowerCmd.Apply<EvasiveArrowSlashPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[DamageKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() =>
            DynamicVars[DamageKey].UpgradeValueBy(2m);
    }
}
