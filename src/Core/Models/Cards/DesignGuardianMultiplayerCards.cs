using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Cards
{
    public sealed class StepForwardForAll : CardModel
    {
        private const string GuardCounterKey = "GuardCounter";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/step_forward_for_all.png");

        public override CardMultiplayerConstraint MultiplayerConstraint =>
            CardMultiplayerConstraint.MultiplayerOnly;

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(12m, ValueProp.Move),
            new PowerVar<GuardCounterPower>(GuardCounterKey, 8m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public StepForwardForAll()
            : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            foreach (Player teammate in LivingTeammates())
            {
                await CreatureCmd.GainBlock(teammate.Creature, DynamicVars.Block, cardPlay);
                await PowerCmd.Apply<GuardCounterPower>(
                    context,
                    teammate.Creature,
                    DynamicVars[GuardCounterKey].BaseValue,
                    Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(4m);
            DynamicVars[GuardCounterKey].UpgradeValueBy(4m);
        }

        private IEnumerable<Player> LivingTeammates() =>
            Owner.Creature.CombatState.Players.Where(player =>
                player != Owner && player.Creature.IsAlive);
    }

    public sealed class GuardianMultiplayerCard : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/guardian_multiplayer_card.png");

        public override CardMultiplayerConstraint MultiplayerConstraint =>
            CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<FortifyPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        };

        public GuardianMultiplayerCard()
            : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PowerCmd.Apply<GuardianMultiplayerPower>(
                context,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }
}

