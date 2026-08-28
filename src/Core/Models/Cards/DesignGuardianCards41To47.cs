using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards
{
    // Card-table ID 41: 风暴袭击
    public sealed class StormAssault : CardModel
    {
        protected override bool ShouldGlowGoldInternal =>
            base.CombatState?.HittableEnemies.Any(enemy => enemy.HasPower<WeakPower>()) ?? false;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(18m, ValueProp.Move),
            new EnergyVar(1)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            base.EnergyHoverTip,
            HoverTipFactory.FromPower<WeakPower>()
        };

        public StormAssault()
            : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            bool targetWasWeak = cardPlay.Target.HasPower<WeakPower>();

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(choiceContext);

            if (targetWasWeak)
            {
                await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Energy.UpgradeValueBy(1m);
        }
    }

    // Card-table ID 42: 唤起风暴
    public sealed class InvokeStorm : CardModel
    {
        protected override bool HasEnergyCostX => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(4m, ValueProp.Move)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        public InvokeStorm()
            : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // Match the original Whirlwind implementation: resolve the engine's
            // effective X value so effects such as Chemical X modify both halves.
            int xValue = ResolveEnergyXValue() + (base.IsUpgraded ? 1 : 0);
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .WithHitCount(xValue)
                .FromCard(this)
                .TargetingAllOpponents(base.CombatState)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(choiceContext);

            // Apply Weak once per point of X instead of as one stacked debuff.
            // This lets each point strip one layer of Artifact before later
            // applications begin adding Weak.
            for (int i = 0; i < xValue; i++)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    base.CombatState.HittableEnemies,
                    1m,
                    base.Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade()
        {
        }
    }

    // Card-table ID 43: 幻影枪
    public sealed class PhantomSpear : CardModel
    {
        private const string ImbalanceKey = "Imbalance";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(9m, ValueProp.Move),
            new PowerVar<PhantomImbalancePower>(ImbalanceKey, 1m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>()
        };

        public PhantomSpear()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            await PowerCmd.Apply<PhantomImbalancePower>(
                choiceContext,
                cardPlay.Target,
                base.DynamicVars[ImbalanceKey].BaseValue,
                base.Owner.Creature,
                this);

            await PhantomImbalancePower.ResolveThreshold(choiceContext, cardPlay.Target);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(3m);
        }
    }

    // Card-table ID 44: 幻影共击
    public sealed class PhantomCoStrike : CardModel
    {
        private const string ImbalanceKey = "Imbalance";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(8m, ValueProp.Move),
            new PowerVar<PhantomImbalancePower>(ImbalanceKey, 1m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>()
        };

        public PhantomCoStrike()
            : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<PhantomCoStrikePower>(
                    choiceContext,
                    cardPlay.Target,
                    base.DynamicVars.Damage.BaseValue,
                    base.Owner.Creature,
                    this);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(3m);
        }
    }

    // Card-table ID 45: 缓步防御
    public sealed class SlowDefend : CardModel
    {
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new BlockVar(5m, ValueProp.Move)
        };

        public SlowDefend()
            : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

            List<CardModel> eligibleCards = PileType.Discard.GetPile(base.Owner).Cards
                .Where(GuardianCardFilters.HasDefendInName)
                .ToList();
            if (eligibleCards.Count == 0)
            {
                return;
            }

            CardModel selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                eligibleCards,
                base.Owner,
                new CardSelectorPrefs(new LocString("cards", "SLOW_DEFEND.selectionScreenPrompt"), 1)))
                .FirstOrDefault();
            if (selected != null)
            {
                await CardPileCmd.Add(selected, PileType.Hand);
            }
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Block.UpgradeValueBy(3m);
        }
    }

    // Card-table ID 46: 铁壁盾防
    public sealed class IronWallDefend : CardModel
    {
        private const string FortifyKey = "Fortify";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<FortifyPower>(FortifyKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<FortifyPower>()
        };

        public IronWallDefend()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<IronWallDefendPower>(
                choiceContext,
                base.Owner.Creature,
                base.DynamicVars[FortifyKey].BaseValue,
                base.Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            base.EnergyCost.UpgradeBy(-1);
        }
    }

    // Card-table ID 47: 风暴化身
    public sealed class StormAvatar : CardModel
    {
        private const string WeakKey = "Weak";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<GuardCounterPower>()
        };

        public StormAvatar()
            : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<StormAvatarPower>(
                choiceContext,
                base.Owner.Creature,
                base.DynamicVars[WeakKey].BaseValue,
                base.Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars[WeakKey].UpgradeValueBy(1m);
        }
    }
}

namespace NightMustStay.Core.Models.Power
{
    public sealed class PhantomImbalancePower : PowerModel
    {
        public const int StacksPerPlayer = 3;

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Description
        {
            get
            {
                LocString description = base.Description;
                int playerCount = CombatManager.Instance.DebugOnlyGetState()?.Players.Count ?? 1;
                description.Add("Threshold", GetThreshold(playerCount));
                return description;
            }
        }

        public static int GetThreshold(int playerCount)
        {
            playerCount = Math.Max(1, playerCount);
            return playerCount == 1
                ? StacksPerPlayer
                : StacksPerPlayer * playerCount - 2;
        }

        public static async Task ResolveThreshold(PlayerChoiceContext context, Creature target)
        {
            PhantomImbalancePower imbalance = target.GetPower<PhantomImbalancePower>();
            int playerCount = Math.Max(1, target.CombatState?.Players.Count ?? 1);
            int stunThreshold = GetThreshold(playerCount);
            if (imbalance == null || imbalance.Amount < stunThreshold)
                return;

            await CreatureCmd.Stun(target);
            await PowerCmd.ModifyAmount(context, imbalance, -stunThreshold, imbalance.Applier, null);
        }
    }

    // Attached to the struck enemy. Each application resolves separately so that
    // every delayed hit also contributes exactly one stack of Imbalance.
    public sealed class PhantomCoStrikePower : PowerModel
    {
        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>()
        };

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (base.Applier == null || side != base.Applier.Side)
            {
                return;
            }

            Flash();
            await CreatureCmd.Damage(
                new BlockingPlayerChoiceContext(),
                base.Owner,
                base.Amount,
                ValueProp.Unpowered,
                base.Applier,
                null);

            if (base.Owner.IsAlive)
            {
                await PowerCmd.Apply<PhantomImbalancePower>(
                    new BlockingPlayerChoiceContext(),
                    base.Owner,
                    1m,
                    base.Applier,
                    null);

                await PhantomImbalancePower.ResolveThreshold(
                    new BlockingPlayerChoiceContext(),
                    base.Owner);
            }

            await PowerCmd.Remove(this);
        }
    }

    // A temporary negative Strength modifier used by Invoke Storm.  The engine's
    // TemporaryStrengthPower restores the Strength at the end of the owner's turn.
    public sealed class InvokeStormStrengthDownPower : TemporaryStrengthPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<InvokeStorm>();

        protected override bool IsPositive => false;
    }

    public sealed class IronWallDefendPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != base.Owner.Side)
            {
                return;
            }

            Flash();
            await PowerCmd.Apply<FortifyPower>(
                new BlockingPlayerChoiceContext(),
                base.Owner,
                base.Amount,
                base.Owner,
                null);
        }
    }

    public sealed class StormAvatarPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public async Task AfterGuardCounterSucceeded(
            PlayerChoiceContext choiceContext,
            Creature counterTarget)
        {
            Flash();
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                counterTarget,
                base.Amount,
                base.Owner,
                null);
        }
    }
}
