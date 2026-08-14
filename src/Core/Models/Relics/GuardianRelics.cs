using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Relics
{
    public abstract class GuardianRelicModel : RelicModel
    {
        protected abstract string GuardianIconName { get; }

        public override string PackedIconPath => $"res://guardian_assets/relics/{GuardianIconName}.png";
        protected override string PackedIconOutlinePath => PackedIconPath;
        protected override string BigIconPath => PackedIconPath;

        // Guardian relic art uses substantially larger filled silhouettes than
        // the base-game atlas icons. Keep the normal inventory flash, but avoid
        // the three-layer additive copy rendered above the player.
        public override bool ShouldFlashOnPlayer => false;
    }

    public sealed class HuntersDarkNight : GuardianRelicModel
    {
        protected override string GuardianIconName => "hunters_dark_night";
        public override RelicRarity Rarity => RelicRarity.Common;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<GuardCounterPower>(10m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public override async Task BeforeCombatStart()
        {
            Flash();
            await PowerCmd.Apply<GuardCounterPower>(new BlockingPlayerChoiceContext(), Owner.Creature,
                DynamicVars[nameof(GuardCounterPower)].BaseValue, Owner.Creature, null);
        }
    }

    public sealed class FlyingFeatherHelm : GuardianRelicModel
    {
        private int _defenseCardsPlayed;
        private bool _isActivating;

        protected override string GuardianIconName => "flying_feather_helm";
        public override RelicRarity Rarity => RelicRarity.Uncommon;
        public override string FlashSfx => "event:/sfx/ui/relic_activate_draw";
        public override bool ShowCounter => true;
        public override int DisplayAmount => _isActivating ? DynamicVars.Cards.IntValue : DefenseCardsPlayed % DynamicVars.Cards.IntValue;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(5) };

        [SavedProperty]
        public int DefenseCardsPlayed
        {
            get => _defenseCardsPlayed;
            set
            {
                AssertMutable();
                _defenseCardsPlayed = value;
                InvokeDisplayAmountChanged();
            }
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner || !GuardianCardFilters.HasDefendInName(cardPlay.Card))
                return;

            DefenseCardsPlayed++;
            if (!CombatManager.Instance.IsInProgress || DefenseCardsPlayed % DynamicVars.Cards.IntValue != 0)
                return;

            _isActivating = true;
            InvokeDisplayAmountChanged();
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
            _isActivating = false;
            InvokeDisplayAmountChanged();
        }
    }

    public sealed class StonePillar : GuardianRelicModel
    {
        private bool _triggeredThisTurn;

        protected override string GuardianIconName => "stone_pillar";
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (participants.Contains(Owner.Creature))
                _triggeredThisTurn = false;
            return Task.CompletedTask;
        }

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
            decimal amount, Creature applier, CardModel cardSource)
        {
            if (_triggeredThisTurn || amount <= 0 || power is not WeakPower || applier != Owner.Creature)
                return;

            _triggeredThisTurn = true;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }

    public sealed class WitchBrooch : GuardianRelicModel
    {
        protected override string GuardianIconName => "witch_brooch";
        public override RelicRarity Rarity => RelicRarity.Rare;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<GuardCounterPower>(6m)
        };
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public override async Task AfterCardPlayed(
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner
                || !CombatManager.Instance.IsInProgress
                || cardPlay.Card is not GuardianConcealedEdgeCard)
            {
                return;
            }

            Flash();
            await PowerCmd.Apply<GuardCounterPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[nameof(GuardCounterPower)].BaseValue,
                Owner.Creature,
                null);
        }
    }

    public sealed class GreenTalisman : GuardianRelicModel
    {
        protected override string GuardianIconName => "green_talisman";
        public override RelicRarity Rarity => RelicRarity.Rare;

        public async Task AfterGuardCounterSucceeded(PlayerChoiceContext choiceContext)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }

    public sealed class GreatshieldTalisman : GuardianRelicModel
    {
        protected override string GuardianIconName => "greatshield_talisman";
        public override RelicRarity Rarity => RelicRarity.Rare;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<FortifyPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        };

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
            decimal amount, Creature applier, CardModel cardSource)
        {
            if (amount <= 0 || power is not FortifyPower || power.Owner != Owner.Creature)
                return;

            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Unpowered, null);
        }
    }

    public sealed class TacticalCompendium : GuardianRelicModel
    {
        protected override string GuardianIconName => "tactical_compendium";
        public override RelicRarity Rarity => RelicRarity.Shop;

        public override async Task BeforeCombatStart()
        {
            CardModel shieldPoke = Owner.Creature.CombatState.CreateCard<ShieldPoke>(Owner);
            CardCmd.Upgrade(shieldPoke);
            Flash();
            await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
}
