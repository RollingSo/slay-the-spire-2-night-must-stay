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
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Nodes.Vfx;

namespace sts2mod.Core.Models.Cards
{
    internal static class GuardianSynthesis
    {
        public static async Task<CardModel> SelectOne(PlayerChoiceContext context, IEnumerable<CardModel> cards, CardModel source, string prompt)
        {
            return (await CardSelectCmd.FromSimpleGrid(context, cards.ToList(), source.Owner,
                new CardSelectorPrefs(new LocString("cards", prompt), 1))).FirstOrDefault();
        }

        public static async Task Consume(PlayerChoiceContext context, params CardModel[] cards)
        {
            foreach (CardModel card in cards.Where(card => card != null).Distinct())
                await CardCmd.Exhaust(context, card);
        }
    }

    // Card-table ID 33: 整备
    public sealed class GuardianPreparation : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(3) };
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { GuardianCardHoverTips.Synthesis, HoverTipFactory.FromCard<DefendGuardian>() };
        public GuardianPreparation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
            CardModel first = await GuardianSynthesis.SelectOne(context, PileType.Hand.GetPile(Owner).Cards, this, "GUARDIAN_PREPARATION.firstSelectionPrompt");
            if (first == null) return;
            CardModel second = await GuardianSynthesis.SelectOne(context, PileType.Hand.GetPile(Owner).Cards.Where(card => card != first), this, "GUARDIAN_PREPARATION.secondSelectionPrompt");
            if (second == null) return;
            await GuardianSynthesis.Consume(context, first, second);
            await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<DefendGuardian>(Owner), PileType.Hand, Owner);
        }
        protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
    }

    // Card-table ID 34: 风旋戟
    public sealed class CycloneHalberd : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/cyclone_halberd.png");

        private const string ImbalanceKey = "Imbalance";
        private const string WeakKey = "Weak";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(12m, ValueProp.Move),
            new PowerVar<PhantomImbalancePower>(ImbalanceKey, 1m),
            new PowerVar<WeakPower>(WeakKey, 2m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromPower<PhantomImbalancePower>(),
            HoverTipFactory.FromPower<WeakPower>()
        };

        public CycloneHalberd() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                .Execute(context);

            await PowerCmd.Apply<PhantomImbalancePower>(
                context,
                cardPlay.Target,
                DynamicVars[ImbalanceKey].BaseValue,
                Owner.Creature,
                this);

            await PhantomImbalancePower.ResolveThreshold(context, cardPlay.Target);

            await PowerCmd.Apply<WeakPower>(
                context,
                cardPlay.Target,
                DynamicVars[WeakKey].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars[ImbalanceKey].UpgradeValueBy(1m);
            DynamicVars[WeakKey].UpgradeValueBy(1m);
        }
    }

    // Card-table ID 35: 冲撞
    public sealed class GuardianCharge : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move) };
        public GuardianCharge() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        }

        public async Task AfterGuardCounterSucceeded()
        {
            if (Pile == null || Pile.Type == PileType.Hand || !Pile.Type.IsCombatPile())
                return;

            await CardPileCmd.Add(this, PileType.Hand);
        }

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
    }

    // Card-table ID 36: 翔空
    public sealed class GuardianSkyward : CardModel
    {
        public override bool GainsBlock => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { base.EnergyHoverTip };
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new EnergyVar(2), new BlockVar(12m, ValueProp.Move) };
        public GuardianSkyward() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(6m);
        }
    }

    // Card-table ID 37: 进化防御
    public sealed class EvolvedDefend : CardModel
    {
        private const string DefendCountKey = "DefendCount";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar(DefendCountKey).WithMultiplier(
                static (card, _) => PileType.Discard.GetPile(card.Owner).Cards.Count(
                    discardCard => discardCard is DefendGuardian))
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            GuardianCardHoverTips.Synthesis,
            HoverTipFactory.FromCard<UltimateDefend>(IsUpgraded)
        };
        public EvolvedDefend() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
        protected override void AddExtraArgsToDescription(LocString description) =>
            description.Add("GeneratedCard", ModelDb.Card<UltimateDefend>().Title + (IsUpgraded ? "+" : string.Empty));
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            CardPile discard = PileType.Discard.GetPile(Owner);
            CardModel defend = await GuardianSynthesis.SelectOne(context, discard.Cards.Where(card => card is DefendGuardian), this, "EVOLVED_DEFEND.defendSelectionPrompt");
            if (defend == null) return;
            CardModel other = await GuardianSynthesis.SelectOne(context, discard.Cards.Where(card => card != defend), this, "EVOLVED_DEFEND.otherSelectionPrompt");
            if (other == null) return;
            await GuardianSynthesis.Consume(context, defend, other);
            CardModel result = CombatState.CreateCard<UltimateDefend>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(result);
            await CardPileCmd.AddGeneratedCardToCombat(result, PileType.Hand, Owner);
        }
        protected override void OnUpgrade() { }
    }

    // Card-table ID 39: 蓄锐
    public sealed class SharpenResolve : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("GuardCounter", 10m) };
        public SharpenResolve() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            decimal amount = DynamicVars["GuardCounter"].BaseValue;
            if (cardPlay.Target.Monster == null || !cardPlay.Target.Monster.IntendsToAttack)
                amount *= 2m;
            await PowerCmd.Apply<GuardCounterPower>(context, Owner.Creature, amount, Owner.Creature, this);
        }
        protected override void OnUpgrade() => DynamicVars["GuardCounter"].UpgradeValueBy(4m);
    }

    // Card-table ID 40: 大龙卷
    public sealed class GreatTornado : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(13m, ValueProp.Move), new PowerVar<WeakPower>("Weak", 3m) };
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WeakPower>() };
        public GreatTornado() : base(1, CardType.Attack, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<GreatTornadoPower>(context, Owner.Creature, DynamicVars.Damage.BaseValue, Owner.Creature, this);
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
    }

    // Card-table ID 48: 圣域
    public sealed class GuardianSanctuary : CardModel
    {
        public override bool GainsBlock => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(14m, ValueProp.Move), new DynamicVar("Heal", 6m) };
        public GuardianSanctuary() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<SanctuaryWatchPower>(context, Owner.Creature, DynamicVars["Heal"].BaseValue, Owner.Creature, this);
        }
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars["Heal"].UpgradeValueBy(4m);
        }
    }

    // Card-table ID 49: 突击
    public sealed class GuardianAssault : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Retain };
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(24m, ValueProp.Move) };
        public GuardianAssault() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
            await PowerCmd.Apply<NoAttacksNextTurnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        }
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
    }

    // Card-table ID 50: 攻守易形
    public sealed class OffenseDefenseShift : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
        public OffenseDefenseShift() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            int block = Owner.Creature.Block;
            if (block <= 0) return;
            await PowerCmd.Apply<GuardCounterPower>(context, Owner.Creature, block, Owner.Creature, this);
        }
        protected override void OnUpgrade() => CardCmd.RemoveKeyword(this, CardKeyword.Exhaust);
    }

    // Card-table ID 51: 极意攻防
    public sealed class UltimateOffenseDefense : CardModel
    {
        private const string CalculatedShieldPokesKey = "CalculatedShieldPokes";

        public override IEnumerable<CardKeyword> CanonicalKeywords => Array.Empty<CardKeyword>();

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new CalculationExtraVar(1m),
            new CalculatedVar(CalculatedShieldPokesKey).WithMultiplier(
                (card, _) =>
                    PileType.Draw.GetPile(card.Owner).Cards.Count(pileCard => pileCard is ShieldPoke)
                    + PileType.Discard.GetPile(card.Owner).Cards.Count(pileCard => pileCard is ShieldPoke))
        };

        public UltimateOffenseDefense() : base(2, CardType.Attack, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
        {
            CardModel[] shieldPokes = PileType.Draw.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Where(card => card is ShieldPoke)
                .ToArray();

            foreach (CardModel poke in shieldPokes)
            {
                await CardPileCmd.Add(poke, PileType.Draw, CardPilePosition.Top);
                await CardPileCmd.AutoPlayFromDrawPile(context, Owner, 1, CardPilePosition.Top, false);
            }
        }
        protected override void OnUpgrade() => CardCmd.ApplyKeyword(this, CardKeyword.Retain);
    }

    // Card-table ID 52: 风缠
    public sealed class SpearPolish : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
        {
            HoverTipFactory.FromCard<ShieldPoke>()
        }.Concat(HoverTipFactory.FromEnchantment<Inky>());

        public SpearPolish() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<SpearPolishPower>(context, Owner.Creature, 1m, Owner.Creature, this);

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 53: 千钧戟
    public sealed class ThousandWeightHalberd : CardModel
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<GuardCounterPower>() };
        public ThousandWeightHalberd() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<ThousandWeightHalberdPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    }

    // Card-table ID 54: 漫步咒魂
    public sealed class WanderingSpellSoul : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Unpowered) };
        public WanderingSpellSoul() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<WanderingSpellSoulPower>(context, Owner.Creature, DynamicVars.Damage.BaseValue, Owner.Creature, this);
        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
    }

    public sealed class StormBirth : CardModel
    {
        public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/guardian/storm_birth.png");

        private const string MultiplierKey = "Multiplier";

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar(MultiplierKey, 3m)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<WeakPower>()
        };

        public StormBirth() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
            await PowerCmd.Apply<StormBirthPower>(
                context,
                Owner.Creature,
                DynamicVars[MultiplierKey].BaseValue,
                Owner.Creature,
                this);

        protected override void OnUpgrade() => DynamicVars[MultiplierKey].UpgradeValueBy(2m);
    }
}

namespace sts2mod.Core.Models.Power
{
    public sealed class GreatTornadoPower : PowerModel
    {
        private sealed class Data
        {
            public CardModel SourceCard;
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

        protected override object InitInternalData() => new Data();

        public override Task AfterApplied(Creature applier, CardModel cardSource)
        {
            GetInternalData<Data>().SourceCard = cardSource;
            return Task.CompletedTask;
        }

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
        {
            if (side != Owner.Side) return;

            Flash();
            BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
            await PowerCmd.Apply<WeakPower>(context, CombatState.HittableEnemies, 3m, Owner, null);

            CardModel sourceCard = GetInternalData<Data>().SourceCard;
            if (sourceCard != null)
            {
                await DamageCmd.Attack(Amount)
                    .FromCard(sourceCard)
                    .WithNoAttackerAnim()
                    .TargetingAllOpponents(CombatState)
                    .WithHitVfxNode(NightreignHitVfx.CreateGuardianWhirlwind)
                    .Execute(context);
            }
            else
            {
                foreach (Creature enemy in CombatState.HittableEnemies.Where(enemy => enemy.IsAlive))
                    NightreignHitVfx.PlayGuardianWhirlwind(enemy);
                await CreatureCmd.Damage(
                    context,
                    CombatState.HittableEnemies,
                    Amount,
                    ValueProp.Move | ValueProp.SkipHurtAnim,
                    Owner,
                    null);
            }

            await PowerCmd.Remove(this);
        }
    }

    public sealed class SanctuaryWatchPower : PowerModel
    {
        private sealed class Data { public int Hp; public bool Tracking; public bool WasAttacked; }
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        protected override object InitInternalData() => new Data();
        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
        {
            if (side == CombatSide.Enemy) { Data d = GetInternalData<Data>(); d.Hp = Owner.CurrentHp; d.Tracking = true; d.WasAttacked = false; }
            return Task.CompletedTask;
        }
        public override Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel card)
        {
            if (target == Owner && dealer?.Side == CombatSide.Enemy && props.IsPoweredAttack()) GetInternalData<Data>().WasAttacked = true;
            return Task.CompletedTask;
        }
        public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (side != CombatSide.Enemy) return;
            await TryHeal();
            await PowerCmd.Remove(this);
        }

        public override async Task AfterCombatEnd(CombatRoom room)
        {
            // A Guard Counter can kill the final enemy during its attack. In
            // that case combat ends before AfterSideTurnEnd, so settle the
            // pending Sanctuary heal here as well.
            await TryHeal();
        }

        private async Task TryHeal()
        {
            Data d = GetInternalData<Data>();
            if (!d.Tracking)
                return;

            d.Tracking = false;
            if (d.WasAttacked && Owner.CurrentHp >= d.Hp)
            {
                Flash();
                await CreatureCmd.Heal(Owner, Amount);
            }
        }
    }

    public sealed class ThousandWeightHalberdPower : PowerModel
    {
        private sealed class Data
        {
            public readonly Dictionary<Creature, decimal> PendingImbalance = new Dictionary<Creature, decimal>();
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData() => new Data();

        public void QueueImbalance(Creature target)
        {
            Data data = GetInternalData<Data>();
            data.PendingImbalance.TryGetValue(target, out decimal pendingAmount);
            data.PendingImbalance[target] = pendingAmount + Amount;
        }

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState combatState)
        {
            if (side != Owner.Side)
                return;

            Data data = GetInternalData<Data>();
            KeyValuePair<Creature, decimal>[] pendingEntries = data.PendingImbalance.ToArray();
            data.PendingImbalance.Clear();
            if (pendingEntries.Length == 0)
                return;

            Flash();
            BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
            foreach (KeyValuePair<Creature, decimal> pending in pendingEntries)
            {
                if (!pending.Key.IsAlive)
                    continue;

                await PowerCmd.Apply<PhantomImbalancePower>(
                    context,
                    pending.Key,
                    pending.Value,
                    Owner,
                    null);
                await PhantomImbalancePower.ResolveThreshold(context, pending.Key);
            }
        }
    }

    public sealed class WanderingSpellSoulPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner.Creature != Owner || !GuardianCardFilters.HasDefendInName(cardPlay.Card)) return;
            Creature[] enemies = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            if (enemies.Length == 0) return;
            Creature target = enemies[0].Monster?.Rng.NextItem(enemies) ?? enemies[0];
            Flash();
            await CreatureCmd.Damage(context, target, Amount, ValueProp.Unpowered, Owner, null);
        }
    }

    public sealed class StormBirthPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterSideTurnStart(
            CombatSide side,
            IReadOnlyList<Creature> creatures,
            ICombatState state)
        {
            if (side != Owner.Side) return;

            Creature[] weakEnemies = CombatState.HittableEnemies
                .Where(enemy => enemy.IsAlive && enemy.GetPower<WeakPower>()?.Amount > 0m)
                .ToArray();
            if (weakEnemies.Length == 0) return;

            Flash();
            BlockingPlayerChoiceContext context = new BlockingPlayerChoiceContext();
            foreach (Creature enemy in weakEnemies)
            {
                decimal weak = enemy.GetPower<WeakPower>()?.Amount ?? 0m;
                NightreignHitVfx.PlayGuardianWhirlwind(enemy);
                await CreatureCmd.Damage(
                    context,
                    enemy,
                    weak * Amount,
                    ValueProp.Unpowered,
                    Owner,
                    null);
            }
        }
    }
}
