using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards
{
    public abstract class GuardianConcealedEdgeCard : CardModel
    {
        protected GuardianConcealedEdgeCard(
            int energyCost,
            CardRarity rarity,
            CardType cardType = CardType.Skill,
            TargetType targetType = TargetType.Self)
            : base(energyCost, cardType, rarity, targetType)
        {
        }

        public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner
                || !GuardianCardFilters.HasDefendInName(cardPlay.Card)
                || Pile == null
                || !Pile.Type.IsCombatPile())
            {
                return Task.CompletedTask;
            }

            EnergyCost.SetUntilPlayed(Math.Max(0, EnergyCost.GetResolved() - 1));
            return Task.CompletedTask;
        }
    }

    // Card-table ID 67: 护身烈风
    public sealed class WardingGale : CardModel
    {
        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/warding_gale.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(10m, ValueProp.Unpowered)
        };

        public WardingGale() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<WardingGalePower>(
                context,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
    }

    // Card-table ID 68: 绝对防御
    public sealed class AbsoluteDefense : GuardianConcealedEdgeCard
    {
        private const string FortifyKey = "Fortify";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/absolute_defense.png");

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(8m, ValueProp.Move),
            new PowerVar<FortifyPower>(FortifyKey, 8m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.Static(StaticHoverTip.Block),
            HoverTipFactory.FromPower<FortifyPower>()
        };

        public AbsoluteDefense() : base(4, CardRarity.Uncommon)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<FortifyPower>(
                context,
                Owner.Creature,
                DynamicVars[FortifyKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2m);
            DynamicVars[FortifyKey].UpgradeValueBy(2m);
        }
    }

    // Card-table ID 69: 旋风
    public sealed class GuardianWhirlwind : GuardianConcealedEdgeCard
    {
        private const string ImbalanceKey = "Imbalance";
        private const string WeakKey = "Weak";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/guardian_whirlwind.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<PhantomImbalancePower>(ImbalanceKey, 1m),
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>(),
            HoverTipFactory.FromPower<WeakPower>()
        };

        public GuardianWhirlwind() : base(4, CardRarity.Rare)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            Creature[] targets = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            foreach (Creature target in targets)
            {
                NightreignHitVfx.PlayGuardianWhirlwind(target);
                await PowerCmd.Apply<PhantomImbalancePower>(
                    context,
                    target,
                    DynamicVars[ImbalanceKey].BaseValue,
                    Owner.Creature,
                    this);

                await PhantomImbalancePower.ResolveThreshold(context, target);

                if (target.IsAlive)
                {
                    await PowerCmd.Apply<WeakPower>(
                        context,
                        target,
                        DynamicVars[WeakKey].BaseValue,
                        Owner.Creature,
                        this);
                }
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars[ImbalanceKey].UpgradeValueBy(1m);
            DynamicVars[WeakKey].UpgradeValueBy(1m);
        }
    }

    // Card-table ID 70: 垫步
    public sealed class Sidestep : CardModel
    {
        private const string CardsKey = "Cards";

        public override string PortraitPath =>
            ImageHelper.GetImagePath("packed/card_portraits/guardian/sidestep.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CardsVar(2)
        };

        public Sidestep() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<SidestepPower>(
                context,
                Owner.Creature,
                DynamicVars[CardsKey].BaseValue,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => DynamicVars[CardsKey].UpgradeValueBy(1m);
    }
}

namespace NightMustStay.Core.Models.Power
{
    public sealed class WardingGalePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task BeforeSideTurnEnd(
            PlayerChoiceContext context,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (!participants.Contains(Owner)
                || CombatManager.Instance.History.CardPlaysFinished.Any(
                    (CardPlayFinishedEntry entry) =>
                        entry.HappenedThisTurn(CombatState)
                        && entry.CardPlay.Card.Type == CardType.Attack
                        && entry.CardPlay.Card.Owner == Owner.Player))
            {
                return;
            }

            Flash();
            foreach (Creature enemy in CombatState.HittableEnemies.Where(enemy => enemy.IsAlive))
                NightreignHitVfx.PlayGuardianWhirlwind(enemy);
            await CreatureCmd.Damage(
                context,
                CombatState.HittableEnemies,
                Amount,
                ValueProp.Unpowered,
                Owner,
                null);
        }
    }

    public sealed class SidestepPower : PowerModel
    {
        private sealed class Data
        {
            public bool DrawExtraNextTurn;
        }

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData() => new Data();

        public override decimal ModifyHandDraw(Player player, decimal count) =>
            player == Owner.Player && GetInternalData<Data>().DrawExtraNextTurn
                ? count + Amount
                : count;

        public override Task AfterSideTurnEnd(
            PlayerChoiceContext context,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (!participants.Contains(Owner))
            {
                return Task.CompletedTask;
            }

            GetInternalData<Data>().DrawExtraNextTurn =
                !CombatManager.Instance.History.CardPlaysFinished.Any(
                    (CardPlayFinishedEntry entry) =>
                        entry.HappenedThisTurn(CombatState)
                        && entry.CardPlay.Card.Type == CardType.Attack
                        && entry.CardPlay.Card.Owner == Owner.Player);
            return Task.CompletedTask;
        }

        public override Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> participants,
            ICombatState combatState)
        {
            Data data = GetInternalData<Data>();
            if (participants.Contains(Owner) && data.DrawExtraNextTurn)
            {
                Flash();
                data.DrawExtraNextTurn = false;
            }

            return Task.CompletedTask;
        }
    }
}
