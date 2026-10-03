using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public static class DuchessReactionRules
{
    public static bool IsDrawnIntoHand(PileType currentPile) => currentPile == PileType.Hand;
    public static bool IsEligibleDraw(bool fromHandDraw, PileType currentPile) =>
        !fromHandDraw && IsDrawnIntoHand(currentPile);
}

public record DuchessEffect(string Kind, decimal Amount, decimal Upgraded, string Condition = "");
public record DuchessCardSpec(int Cost, CardType Type, CardRarity Rarity,
    DuchessEffect[] Effects, int Hits = 1, bool All = false, bool Exhaust = false,
    bool Retain = false, bool UpgradeTokens = false, bool Reaction = false,
    bool ShuffleSelf = false, bool UpgradeRetain = false, bool UpgradeInnate = false,
    bool UpgradeRemoveExhaust = false, int Moment = -1, int UpgradedMoment = -1,
    int UpgradeCost = -1, int MomentCostReduction = 0, bool TargetSelf = false,
    int RestageDivisor = 0, int SecondaryMoment = -1, bool XCost = false,
    bool UpgradeX = false, int UpgradeHits = -1, int ConcealedCostReduction = 0,
    bool MultiplayerOnly = false, bool ConcealedTripleDamage = false,
    bool MomentCostReductionDynamic = false);

public abstract class DuchessCard : CardModel
{
    [SavedProperty]
    public int PendingGlintstoneDamage { get; set; }
    [SavedProperty]
    public int PendingPiercerDamage { get; set; }
    [SavedProperty]
    public int PendingNextCombatStrength { get; set; }
    internal DuchessCardSpec Spec => DuchessCardCatalog.All[GetType().Name];
    protected DuchessCard(string key) : base(DuchessCardCatalog.All[key].Cost,
        DuchessCardCatalog.All[key].Type, DuchessCardCatalog.All[key].Rarity, TargetFor(key)) { }

    private static TargetType TargetFor(string key)
    {
        DuchessCardSpec spec = DuchessCardCatalog.All[key];
        if (spec.TargetSelf) return TargetType.Self;
        if (spec.All) return TargetType.AllEnemies;
        return spec.Type == CardType.Attack || spec.Effects.Any(e => e.Kind is "Weak" or "Vulnerable")
            ? TargetType.AnyEnemy : TargetType.Self;
    }

    public override string PortraitPath => this switch
    {
        DuchessElegantBearing => "res://images/packed/card_portraits/duchess/duchess_elegant_bearing.png",
        DuchessDodge => "res://images/packed/card_portraits/duchess/duchess_dodge.png",
        _ => $"res://images/packed/card_portraits/duchess/{Id.Entry.ToLowerInvariant()}.png",
    };
    protected override bool HasEnergyCostX => Spec.XCost;
    // A card without an authored upgrade must not offer a no-op upgrade.
    public override int MaxUpgradeLevel => Spec.Effects.Any(e => e.Amount != e.Upgraded)
        || Spec.UpgradeTokens || Spec.UpgradeX || Spec.UpgradeRetain || Spec.UpgradeInnate
        || Spec.UpgradeRemoveExhaust || Spec.UpgradedMoment >= 0 || Spec.UpgradeCost >= 0
        || Spec.UpgradeHits >= 0 ? 1 : 0;
    protected override bool IsPlayable => this is not DuchessFallingMagic && base.IsPlayable;
    public override CardPoolModel Pool => this is DuchessDodge or DuchessRadiantBlade
        ? ModelDb.CardPool<TokenCardPool>() : base.Pool;
    public override CardPoolModel VisualCardPool => this is DuchessDodge or DuchessRadiantBlade
        ? ModelDb.CardPool<ColorlessCardPool>() : base.VisualCardPool;
    public override bool GainsBlock => Spec.Effects.Any(e => e.Kind is "Block" or "AllyBlock" or "HandToDrawTopBlock");

    public override async Task BeforeCombatStart()
    {
        if (this is not DuchessPhantomKiller || Pile?.Type != PileType.Deck || PendingNextCombatStrength <= 0)
            return;
        int strength = PendingNextCombatStrength;
        PendingNextCombatStrength = 0;
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner.Creature,
            strength, Owner.Creature, this);
    }
    public override CardMultiplayerConstraint MultiplayerConstraint => Spec.MultiplayerOnly || this is DuchessFinale
        ? CardMultiplayerConstraint.MultiplayerOnly : base.MultiplayerConstraint;
    protected override HashSet<CardTag> CanonicalTags => GetType() == typeof(DuchessStrike)
        ? new() { CardTag.Strike } : GetType() == typeof(DuchessDefend)
            ? new() { CardTag.Defend } : new();

    public override HashSet<CardKeyword> CanonicalKeywords
    {
        get
        {
            var result = new HashSet<CardKeyword>();
            if (Spec.Exhaust && !(IsUpgraded && Spec.UpgradeRemoveExhaust)) result.Add(CardKeyword.Exhaust);
            if (Spec.Retain || (IsUpgraded && Spec.UpgradeRetain)) result.Add(CardKeyword.Retain);
            if (IsUpgraded && Spec.UpgradeInnate) result.Add(CardKeyword.Innate);
            return result;
        }
    }

    internal int RequiredMoment => IsUpgraded && Spec.UpgradedMoment >= 0 ? Spec.UpgradedMoment : Spec.Moment;
    internal bool IsMomentActive => IsMoment(RequiredMoment) || IsMoment(Spec.SecondaryMoment);
    private bool IsMoment(int moment) => moment >= 0 && DuchessMomentPower.Current(Owner) == moment;
    public bool HasReaction => Spec.Reaction;
    private bool IsConcealmentConditionActive =>
        Owner?.Creature?.GetPower<DuchessConcealmentPower>() is { Amount: > 0 }
        && (Spec.ConcealedTripleDamage || Spec.ConcealedCostReduction > 0
            || Spec.Effects.Any(effect => effect.Condition == "concealed"
                || effect.Kind is "ConcealedBonusDamage" or "ConcealedStrength" or "ConcealedKillNextCombatStrength"));
    protected override bool ShouldGlowGoldInternal => IsMomentActive || IsConcealmentConditionActive;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DuchessEffect effect in Spec.Effects)
            {
                if (effect.Kind == "Damage" && Spec.RestageDivisor > 0)
                {
                    yield return new CalculationBaseVar(effect.Amount);
                    yield return new ExtraDamageVar(1m);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => card is DuchessCard { Spec.RestageDivisor: > 0 } duchess
                            && duchess.IsMomentActive
                            ? decimal.Floor(DuchessMomentPower.DamageDealtThisTurn(card) / duchess.Spec.RestageDivisor)
                            : 0m);
                    continue;
                }
                if (effect.Kind == "Damage" && (Spec.ConcealedTripleDamage
                    || Spec.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage")))
                {
                    yield return new CalculationBaseVar(effect.Amount);
                    yield return new ExtraDamageVar(Spec.ConcealedTripleDamage ? 1m
                        : Spec.Effects.First(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage").Amount);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => card is DuchessCard duchess
                            ? duchess.Spec.ConcealedTripleDamage
                                && card.Owner?.Creature?.HasPower<DuchessConcealmentPower>() == true
                                ? 2m * card.DynamicVars["CalculationBase"].BaseValue
                                : duchess.Spec.Effects.Any(e => e.Kind == "ConcealedBonusDamage")
                                    && card.Owner?.Creature?.HasPower<DuchessConcealmentPower>() == true
                                    || duchess.Spec.Effects.Any(e => e.Kind == "MomentBonusDamage")
                                    && duchess.IsMomentActive ? 1m : 0m
                            : 0m);
                    continue;
                }
                if (effect.Kind == "MomentDamage")
                {
                    yield return new CalculationBaseVar(0m);
                    yield return new ExtraDamageVar(effect.Amount > 0 ? effect.Amount : 1m);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => DuchessMomentPower.Current(card.Owner));
                    continue;
                }
                if (effect.Kind == "RewindDamage")
                {
                    yield return new CalculationBaseVar(6m);
                    yield return new ExtraDamageVar(effect.Amount);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => DuchessMomentPower.Current(card.Owner));
                    continue;
                }
                if (effect.Kind == "RestageAoe")
                {
                    yield return new DynamicVar("RestageAoe", effect.Amount);
                    yield return new CalculationBaseVar(0m);
                    yield return new ExtraDamageVar(1m);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => decimal.Floor(DuchessMomentPower.DamageDealtThisTurn(card)
                            / card.DynamicVars["RestageAoe"].BaseValue));
                    continue;
                }
                yield return effect.Kind switch
                {
                    "Damage" => new DamageVar(effect.Amount, ValueProp.Move),
                    "AoeDamage" or "ExtraDamage" => new DynamicVar(effect.Kind, effect.Amount),
                    "Block" or "AllyBlock" => new BlockVar(effect.Amount, ValueProp.Move),
                    "Weak" or "WeakAll" => new PowerVar<WeakPower>(effect.Kind, effect.Amount),
                    "Vulnerable" => new PowerVar<VulnerablePower>(effect.Kind, effect.Amount),
                    "Strength" => new PowerVar<StrengthPower>(effect.Kind, effect.Amount),
                    "TemporaryStrength" => new PowerVar<StrengthPower>(effect.Kind, effect.Amount),
                    "Intangible" => new PowerVar<IntangiblePower>(effect.Kind, effect.Amount),
                    "Dexterity" => new PowerVar<DexterityPower>(effect.Kind, effect.Amount),
                    "Energy" or "NextTurnEnergy" or "FutureMomentEnergy" or "MomentEffectEnergy" or "MomentFiveFirstEnergy" =>
                        new EnergyVar(effect.Kind, (int)effect.Amount),
                    "TurnStartSwap" => new PowerVar<DuchessTurnStartSwapPower>(effect.Kind, effect.Amount),
                    "Concealment" => new PowerVar<DuchessConcealmentPower>(effect.Kind, effect.Amount),
                    "ReactionDrawBlock" => new PowerVar<DuchessReactionDrawBlockPower>(effect.Kind, effect.Amount),
                    "RadiantBladeGrowth" => new PowerVar<DuchessRadiantBladeGrowthPower>(effect.Kind, effect.Amount),
                    "EndTurnDodge" => new PowerVar<DuchessEndTurnDodgePower>(effect.Kind, effect.Amount),
                    "MomentFiveBlock" => new PowerVar<DuchessMomentFiveBlockPower>(effect.Kind, effect.Amount),
                    "RestageEndTurnAoe" => new PowerVar<DuchessEternalRestagePower>(effect.Kind, effect.Amount),
                    "ReactionBlock" => new PowerVar<DuchessReactionBlockPower>(effect.Kind, effect.Amount),
                    "ReactionDraw" => new PowerVar<DuchessReactionDrawPower>(effect.Kind, effect.Amount),
                    "MomentFiveDraw" => new PowerVar<DuchessMomentFiveDrawPower>(effect.Kind, effect.Amount),
                    "DodgeMoment" => new PowerVar<DuchessDodgeMomentPower>(effect.Kind, effect.Amount),
                    "ShuffleBlock" => new PowerVar<DuchessShuffleBlockPower>(effect.Kind, effect.Amount),
                    "ZeroCostAttackBonus" => new PowerVar<DuchessZeroCostAttackPower>(effect.Kind, effect.Amount),
                    "DodgePlayAoe" => new PowerVar<DuchessGracefulSwordDancePower>(effect.Kind, effect.Amount),
                    "ConcealedKillNextCombatStrength" => new PowerVar<DuchessPhantomKillerPower>(effect.Kind, effect.Amount),
                    _ => new DynamicVar(effect.Kind, effect.Amount),
                };
            }
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (Spec.Reaction) yield return HoverTipFactory.FromPower<DuchessReactionDescriptionPower>();
            if (Spec.Moment >= 0) yield return HoverTipFactory.FromPower<DuchessMomentDescriptionPower>();
            foreach (DuchessEffect effect in Spec.Effects)
            {
                if (effect.Kind is "DodgeToDraw" or "DodgeToDrawTop" or "DodgeToHand" or "AllyDodge"
                    or "EndTurnDodge" or "AllyDodgeDrawX" or "TransformDrawToDodge")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessDodge>(IsUpgraded && Spec.UpgradeTokens))
                        yield return tip;
                if (effect.Kind is "RadiantBladeToDraw" or "RadiantBladeToHand" or "RadiantBladeTurns"
                    or "InstinctRadiantBladesToDraw" or "FullBlockRadiantBlade" or "RadiantBladeGrowth" or "TransformDrawToRadiantBlade")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessRadiantBlade>(
                                 effect.Kind is "RadiantBladeToDraw" or "RadiantBladeToHand" or "InstinctRadiantBladesToDraw" or "TransformDrawToRadiantBlade" && IsUpgraded && Spec.UpgradeTokens))
                        yield return tip;
                if (effect.Kind == "InstinctRadiantBladesToDraw")
                    foreach (IHoverTip tip in HoverTipFactory.FromEnchantment<Instinct>())
                        yield return tip;
                if (effect.Kind is "Weak" or "WeakAll") yield return HoverTipFactory.FromPower<WeakPower>();
                if (effect.Kind == "Vulnerable") yield return HoverTipFactory.FromPower<VulnerablePower>();
                if (effect.Kind == "Strength") yield return HoverTipFactory.FromPower<StrengthPower>();
                if (effect.Kind is "RewindDamage" or "RememberMoment" or "MomentEffectEnergy")
                    yield return HoverTipFactory.FromPower<DuchessMomentDescriptionPower>();
                if (effect.Kind == "MomentEffectEnergy") yield return HoverTipFactory.FromPower<DuchessEternalFormPower>();
                if (effect.Kind == "ConcealedStrength")
                {
                    yield return HoverTipFactory.FromPower<DuchessConcealmentPower>();
                    yield return HoverTipFactory.FromPower<StrengthPower>();
                    yield return HoverTipFactory.FromPower<DuchessShadowSwordPower>();
                }
                if (effect.Kind == "TemporaryStrength") yield return HoverTipFactory.FromPower<StrengthPower>();
                if (effect.Kind == "Intangible") yield return HoverTipFactory.FromPower<IntangiblePower>();
                if (effect.Kind == "AllyIntangible") yield return HoverTipFactory.FromPower<IntangiblePower>();
                if (effect.Kind == "TurnStartSwap") yield return HoverTipFactory.FromPower<DuchessTurnStartSwapPower>();
                if (effect.Kind is "Concealment" or "LoseConcealment" || effect.Condition == "concealed" || Spec.ConcealedTripleDamage)
                    yield return HoverTipFactory.FromPower<DuchessConcealmentPower>();
                if (effect.Kind is "TransformDrawToDodge" or "TransformDrawToRadiantBlade")
                    yield return new HoverTip(new LocString("cards", "DUCHESS_TRANSFORM.title"),
                        new LocString("cards", "DUCHESS_TRANSFORM.description"));
                if (effect.Kind == "ExhaustHandUpTo")
                    yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
                if (effect.Kind == "ReactionDrawBlock") yield return HoverTipFactory.FromPower<DuchessReactionDrawBlockPower>();
                if (effect.Kind == "RadiantBladeGrowth") yield return HoverTipFactory.FromPower<DuchessRadiantBladeGrowthPower>();
                if (effect.Kind == "EndTurnDodge") yield return HoverTipFactory.FromPower<DuchessEndTurnDodgePower>();
                if (effect.Kind == "MomentFiveFirstEnergy") yield return HoverTipFactory.FromPower<DuchessBeatPower>();
                if (effect.Kind == "MomentFiveBlock") yield return HoverTipFactory.FromPower<DuchessMomentFiveBlockPower>();
                if (effect.Kind == "Dexterity") yield return HoverTipFactory.FromPower<DexterityPower>();
                if (effect.Kind == "ReactionBlock") yield return HoverTipFactory.FromPower<DuchessReactionBlockPower>();
                if (effect.Kind == "ReactionDraw") yield return HoverTipFactory.FromPower<DuchessReactionDrawPower>();
                if (effect.Kind == "TransformStrike")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessCarianSlicer>())
                        yield return tip;
                if (effect.Kind == "MomentFiveDraw") yield return HoverTipFactory.FromPower<DuchessMomentFiveDrawPower>();
                if (effect.Kind == "EndTurnMomentBlock") yield return HoverTipFactory.FromPower<DuchessEndTurnMomentBlockPower>();
                if (effect.Kind == "DodgeMoment") yield return HoverTipFactory.FromPower<DuchessDodgeMomentPower>();
                if (effect.Kind == "ShuffleBlock") yield return HoverTipFactory.FromPower<DuchessShuffleBlockPower>();
                if (effect.Kind == "ZeroCostAttackBonus") yield return HoverTipFactory.FromPower<DuchessZeroCostAttackPower>();
                if (effect.Kind == "DodgePlayAoe") yield return HoverTipFactory.FromPower<DuchessGracefulSwordDancePower>();
                if (effect.Kind == "ConcealedKillNextCombatStrength") yield return HoverTipFactory.FromPower<DuchessPhantomKillerPower>();
            }
        }
    }

    protected override void OnUpgrade()
    {
        if (Spec.UpgradeRetain) AddKeyword(CardKeyword.Retain);
        if (Spec.UpgradeInnate) AddKeyword(CardKeyword.Innate);
        if (Spec.UpgradeRemoveExhaust) RemoveKeyword(CardKeyword.Exhaust);
        foreach (DuchessEffect effect in Spec.Effects)
        {
            if (effect.Upgraded == effect.Amount) continue;
            string key = effect.Kind == "Damage" && (Spec.RestageDivisor > 0
                    || Spec.ConcealedTripleDamage || Spec.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage"))
                ? "CalculationBase" : effect.Kind is "ConcealedBonusDamage" or "MomentBonusDamage" ? "ExtraDamage"
                : effect.Kind == "RewindDamage" ? "ExtraDamage"
                : effect.Kind == "AllyBlock" ? "Block" : effect.Kind;
            if (DynamicVars.TryGetValue(key, out DynamicVar variable))
                variable.UpgradeValueBy(effect.Upgraded - effect.Amount);
        }
        if (Spec.UpgradeCost >= 0) EnergyCost.UpgradeBy(Spec.UpgradeCost - Spec.Cost);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && card.Pile?.Type == PileType.Draw
            && oldPileType is PileType.Hand or PileType.Discard)
        {
            var growth = Spec.Effects.FirstOrDefault(effect => effect.Kind == "ShuffleGrowth");
            if (growth != null)
                DynamicVars.Damage.BaseValue += DynamicVars["ShuffleGrowth"].BaseValue;
            var falling = Spec.Effects.FirstOrDefault(effect => effect.Kind == "ShuffleAoeDamage");
            if (falling != null && CombatState.HittableEnemies.Any(enemy => enemy.IsAlive))
                await DamageCmd.Attack(DynamicVars["ShuffleAoeDamage"].BaseValue)
                    .CompatFromCard(this).TargetingAllOpponents(CombatState)
                    .Execute(new BlockingPlayerChoiceContext());
        }
    }

    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        // The native draw hook distinguishes the fixed turn-opening hand from
        // all other draws, including draws made during turn-start effects.
        if (card != this || !DuchessReactionRules.IsDrawnIntoHand(card.Pile?.Type ?? PileType.None))
            return Task.CompletedTask;
        if (this is DuchessCarianPiercer)
        {
            int boost = decimal.ToInt32(DynamicVars["DrawReactionDamageBoost"].BaseValue);
            PendingPiercerDamage += boost;
            DynamicVars.Damage.BaseValue += boost;
        }
        if (Spec.Reaction && DuchessReactionRules.IsEligibleDraw(fromHandDraw, card.Pile?.Type ?? PileType.None))
            EnergyCost.AddUntilPlayed(-1, true);
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (GainsBlock)
            NightMustStay.Core.Patches.DuchessAnimationPatch.PlayBlock(Owner.Creature);
        CardPlayFinishedEntry previous = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.HappenedThisTurn(CombatState) && entry.CardPlay.Card.Owner == Owner);
        var conditions = new Dictionary<string, bool>
        {
            [""] = true,
            ["first"] = previous == null,
            ["afterSkill"] = previous?.CardPlay.Card.Type == CardType.Skill,
            ["afterAttack"] = previous?.CardPlay.Card.Type == CardType.Attack,
            ["moment"] = IsMomentActive,
            ["moment2"] = IsMoment(2),
            ["moment4"] = IsMoment(4),
            ["concealed"] = Owner.Creature.HasPower<DuchessConcealmentPower>(),
        };

        foreach (DuchessEffect effect in Spec.Effects)
        {
            if (!conditions.TryGetValue(effect.Condition, out bool met) || !met) continue;
            string key = effect.Kind == "AllyBlock" ? "Block" : effect.Kind;
            decimal amount = effect.Kind == "Damage" && (Spec.RestageDivisor > 0
                    || Spec.ConcealedTripleDamage || Spec.Effects.Any(e => e.Kind is "ConcealedBonusDamage" or "MomentBonusDamage"))
                ? DynamicVars.CalculatedDamage.Calculate(play.Target)
                : effect.Kind is "MomentDamage" or "RewindDamage" ? DynamicVars.CalculatedDamage.Calculate(play.Target)
                : DynamicVars.TryGetValue(key, out DynamicVar variable) ? variable.BaseValue : effect.Amount;
            Creature[] enemies = Spec.All
                ? CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray()
                : play.Target is { IsAlive: true } target && target.Side != Owner.Creature.Side
                    ? new[] { target } : Array.Empty<Creature>();

            switch (effect.Kind)
            {
                case "Damage":
                    if (DynamicVars.TryGetValue("ShuffleHandDamage", out DynamicVar shuffleDamage))
                    {
                        CardPile hand = PileType.Hand.GetPile(Owner);
                        var selected = (await CardSelectCmd.FromCombatPile(context, hand, Owner,
                            new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_TO_SHUFFLE"), 0, hand.Cards.Count))).ToArray();
                        foreach (CardModel card in selected)
                            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random, this);
                        amount += selected.Length * shuffleDamage.BaseValue;
                    }
                    if (this is DuchessMidnightWaltz)
                    {
                        await NightMustStay.Core.Nodes.Vfx.DuchessMidnightWaltzVfx.PlayPrelude(Owner.Creature);
                        await DamageCmd.Attack(amount).CompatFromCard(this)
                            .TargetingAllOpponents(CombatState)
                            .WithHitVfxNode(NightMustStay.Core.Nodes.Vfx.DuchessMidnightWaltzVfx.CreateImpact)
                            .WithHitFx(null, null, "blunt_attack.mp3")
                            .Execute(context);
                        break;
                    }
                    int hits = IsUpgraded && Spec.UpgradeHits > 0 ? Spec.UpgradeHits : Spec.Hits;
                    if (Spec.All)
                    {
                        // Match native DaggerSpray: one AOE attack owns every hit,
                        // so targeting and attack-completion hooks retain their semantics.
                        await DamageCmd.Attack(amount).WithHitCount(hits).CompatFromCard(this)
                            .TargetingAllOpponents(CombatState)
                            .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    }
                    else
                    {
                        for (int i = 0; i < hits; i++)
                            foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                                await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(enemy)
                                    .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    }
                    if (this is DuchessGlintstoneHail && PendingGlintstoneDamage > 0)
                    {
                        DynamicVars.Damage.BaseValue -= PendingGlintstoneDamage;
                        PendingGlintstoneDamage = 0;
                    }
                    if (this is DuchessCarianPiercer && PendingPiercerDamage > 0)
                    {
                        DynamicVars.Damage.BaseValue -= PendingPiercerDamage;
                        PendingPiercerDamage = 0;
                    }
                    break;
                case "MomentDamage":
                    int momentHits = Spec.XCost
                        ? ResolveEnergyXValue() + (IsUpgraded && Spec.UpgradeX ? 1 : 0)
                        : 1 + (conditions["moment"] && DynamicVars.TryGetValue("MomentExtraHits", out DynamicVar extraHits)
                            ? (int)extraHits.BaseValue : 0);
                    foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                        await DamageCmd.Attack(amount).WithHitCount(momentHits).CompatFromCard(this).Targeting(enemy)
                            .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    break;
                case "MomentExtraHits": break; // Applied to the single native multi-hit attack above.
                case "AoeDamage":
                    await DamageCmd.Attack(amount).CompatFromCard(this)
                        .TargetingAllOpponents(CombatState).Execute(context);
                    break;
                case "ExtraDamage":
                    foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                        await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(enemy)
                            .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    break;
                case "Block": await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play); break;
                case "AllyBlock":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await CreatureCmd.GainBlock(ally.Creature, DynamicVars.Block, play);
                    break;
                case "Draw": await CardPileCmd.Draw(context, amount, Owner); break;
                case "Energy": await PlayerCmd.GainEnergy(amount, Owner); break;
                case "Weak": foreach (Creature enemy in enemies) await Apply<WeakPower>(context, enemy, amount); break;
                case "WeakAll":
                    foreach (Creature enemy in CombatState.HittableEnemies.Where(enemy => enemy.IsAlive))
                        await Apply<WeakPower>(context, enemy, amount);
                    break;
                case "Vulnerable": foreach (Creature enemy in enemies) await Apply<VulnerablePower>(context, enemy, amount); break;
                case "Strength": await Apply<StrengthPower>(context, Owner.Creature, amount); break;
                case "TemporaryStrength":
                    await Apply<DuchessTemporaryStrengthDownPower>(context, Owner.Creature, amount);
                    break;
                case "Intangible": await Apply<IntangiblePower>(context, Owner.Creature, amount); break;
                case "AllyIntangible":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await Apply<IntangiblePower>(context, ally.Creature, amount);
                    break;
                case "Dexterity": await Apply<DexterityPower>(context, Owner.Creature, amount); break;
                case "DodgeToDraw": await AddDodges(Owner, amount, PileType.Draw); break;
                case "DodgeToDrawTop": await AddDodges(Owner, amount, PileType.Draw, CardPilePosition.Top); break;
                case "DodgeToHand": await AddDodges(Owner, amount, PileType.Hand); break;
                case "AllyDodge":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await AddDodges(ally, amount, PileType.Hand);
                    break;
                case "AdvanceMoment": await DuchessMomentPower.Advance(context, Owner.Creature, (int)amount, this); break;
                case "RewindDamage":
                    int rewindSteps = DuchessMomentPower.Current(Owner);
                    for (int step = rewindSteps - 1; step >= 0; step--)
                        await DuchessMomentPower.Set(context, Owner.Creature, step, this);
                    if (play.Target is { IsAlive: true } rewindTarget)
                        await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(rewindTarget)
                            .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    (await DuchessMomentPower.Ensure(context, Owner.Creature)).SetAfterCurrentCard(0);
                    break;
                case "RememberMoment":
                    (await DuchessMomentPower.Ensure(context, Owner.Creature)).SkipNextTurnReset = true;
                    break;
                case "MomentEffectEnergy": await Apply<DuchessEternalFormPower>(context, Owner.Creature, amount); break;
                case "ConcealedStrength": await Apply<DuchessShadowSwordPower>(context, Owner.Creature, amount); break;
                case "SetMoment":
                case "ReturnMoment":
                    (await DuchessMomentPower.Ensure(context, Owner.Creature)).SetAfterCurrentCard((int)amount);
                    break;
                case "ShuffleHand": await ShuffleSelected(context, PileType.Hand.GetPile(Owner), (int)amount); break;
                case "ShuffleDiscard": await ShuffleSelected(context, PileType.Discard.GetPile(Owner), (int)amount); break;
                case "ShuffleDiscardAll": await CardPileCmd.Shuffle(context, Owner); break;
                case "RewindTurn": await (await DuchessMomentPower.Ensure(context, Owner.Creature)).RestoreTurnStart(context, this); break;
                case "TurnStartSwap": await Apply<DuchessTurnStartSwapPower>(context, Owner.Creature, amount); break;
                case "Concealment": await Apply<DuchessConcealmentPower>(context, Owner.Creature, amount); break;
                case "LoseConcealment":
                    if (Owner.Creature.GetPower<DuchessConcealmentPower>() is { } concealment)
                        await Apply<DuchessConcealmentPower>(context, Owner.Creature, -Math.Min(amount, concealment.Amount));
                    break;
                case "ExhaustHandUpTo":
                    CardPile exhaustHand = PileType.Hand.GetPile(Owner);
                    int exhaustMax = Math.Min((int)amount, exhaustHand.Cards.Count);
                    if (exhaustMax > 0)
                        foreach (CardModel card in (await CardSelectCmd.FromCombatPile(context, exhaustHand, Owner,
                                     new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, exhaustMax))).ToArray())
                            await CardCmd.Exhaust(context, card);
                    break;
                case "ReactionDrawBlock": await Apply<DuchessReactionDrawBlockPower>(context, Owner.Creature, amount); break;
                case "RadiantBladeGrowth": await Apply<DuchessRadiantBladeGrowthPower>(context, Owner.Creature, amount); break;
                case "FullBlockRadiantBlade": await Apply<DuchessFullBlockRadiantBladePower>(context, Owner.Creature, amount); break;
                case "EndTurnDodge":
                    await Apply<DuchessEndTurnDodgePower>(context, Owner.Creature, amount);
                    if (IsUpgraded && Spec.UpgradeTokens)
                        Owner.Creature.GetPower<DuchessEndTurnDodgePower>().UpgradedDodgeCount += amount;
                    break;
                case "EndTurnRetain": await Apply<DuchessEndTurnRetainPower>(context, Owner.Creature, amount); break;
                case "MomentFiveFirstEnergy": await Apply<DuchessBeatPower>(context, Owner.Creature, amount); break;
                case "MomentFiveBlock": await Apply<DuchessMomentFiveBlockPower>(context, Owner.Creature, amount); break;
                case "RestageEndTurnAoe": await Apply<DuchessEternalRestagePower>(context, Owner.Creature, amount); break;
                case "ReplayMomentThree": await Apply<DuchessReplayMomentThreePower>(context, Owner.Creature, amount); break;
                case "AllyDodgeDrawX": await AllyDodgeDrawX(context); break;
                case "DrawReactionFromPile": await DrawReactionFromPile(context); break;
                case "MomentTwelveEndTurn": break;
                case "ConcealedBonusDamage": break;
                case "MomentBonusDamage": break;
                case "DrawReactionDamageBoost": break;
                case "HandToDrawTopBlock": await HandToDrawTopBlock(context, amount); break;
                case "DrawUntilMomentHandSize":
                    while (PileType.Hand.GetPile(Owner).Cards.Count < DuchessMomentPower.Current(Owner)
                        && PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand)
                    {
                        CardModel drawn = await CardPileCmd.Draw(context, Owner);
                        if (drawn == null) break;
                    }
                    break;
                case "ReactionBlock": await Apply<DuchessReactionBlockPower>(context, Owner.Creature, amount); break;
                case "ReactionDraw": await Apply<DuchessReactionDrawPower>(context, Owner.Creature, amount); break;
                case "ZeroCostAttackBonus": await Apply<DuchessZeroCostAttackPower>(context, Owner.Creature, amount); break;
                case "DodgePlayAoe": await Apply<DuchessGracefulSwordDancePower>(context, Owner.Creature, amount); break;
                case "ConcealedKillNextCombatStrength": await Apply<DuchessPhantomKillerPower>(context, Owner.Creature, amount); break;
                // Passive: DuchessMomentPower moves the card when Moment reaches 5.
                case "ReturnSelfToHand": break;
                case "ShuffleHandAllDraw":
                    CardModel[] handToShuffle = PileType.Hand.GetPile(Owner).Cards.Where(card => card != this).ToArray();
                    if (handToShuffle.Length > 0)
                    {
                        await CardPileCmd.Add(handToShuffle, PileType.Draw, CardPilePosition.Random, this);
                        await CardPileCmd.Draw(context, handToShuffle.Length, Owner);
                    }
                    break;
                case "TransformStrike": await TransformStrikeInDraw(context); break;
                case "ChooseDrawToTop": await ChooseDrawToTop(context); break;
                case "EndTurnMomentBlock": await Apply<DuchessEndTurnMomentBlockPower>(context, Owner.Creature, amount); break;
                case "MomentFiveDraw": await Apply<DuchessMomentFiveDrawPower>(context, Owner.Creature, amount); break;
                case "DodgeMoment": await Apply<DuchessDodgeMomentPower>(context, Owner.Creature, amount); break;
                case "ShuffleBlock": await Apply<DuchessShuffleBlockPower>(context, Owner.Creature, amount); break;
                case "RestageAoe":
                    decimal restageDamage = DynamicVars.CalculatedDamage.Calculate(null);
                    if (restageDamage > 0)
                        await DamageCmd.Attack(restageDamage).CompatFromCard(this).TargetingAllOpponents(CombatState)
                            .WithHitVfxNode(target => NightMustStay.Core.Nodes.Vfx.DuchessAttackEffects.Create(this, target)).Execute(context);
                    break;
                case "NextTurnEnergy": await Apply<DuchessNextTurnEnergyPower>(context, Owner.Creature, amount); break;
                case "NextTurnDraw": await Apply<DuchessNextTurnDrawPower>(context, Owner.Creature, amount); break;
                case "NextTurnEnergyAndDraw":
                    await Apply<DuchessNextTurnEnergyPower>(context, Owner.Creature, 1m);
                    await Apply<DuchessNextTurnDrawPower>(context, Owner.Creature, amount);
                    break;
                case "FutureMomentEnergy":
                    await Apply<DuchessFutureMomentEnergyPower>(context, Owner.Creature, amount);
                    break;
                case "RadiantBladeToDraw": await AddRadiantBlades(Owner, amount, PileType.Draw); break;
                case "InstinctRadiantBladesToDraw": await AddInstinctRadiantBladesToDraw(amount); break;
                case "RadiantBladeToHand": await AddRadiantBlades(Owner, amount, PileType.Hand); break;
                case "RadiantBladeTurns":
                    int turns = ResolveEnergyXValue() + (IsUpgraded && Spec.UpgradeX ? 1 : 0);
                    if (turns > 0)
                        await Apply<DuchessRadiantBladeTurnsPower>(context, Owner.Creature, turns);
                    break;
                case "DodgeCurrentMoment": await AddDodges(Owner, DuchessMomentPower.Current(Owner), PileType.Draw); break;
                case "DrawCurrentMoment": await CardPileCmd.Draw(context, DuchessMomentPower.Current(Owner), Owner); break;
                // Replay is a declarative marker consumed by
                // DuchessMomentPower.ModifyCardPlayCount. The engine then
                // creates each replay as its own native CardPlay.
                case "Replay": break;
                case "ShuffleGrowth":
                case "ShuffleHandDamage": break;
                case "TransformDrawToDodge":
                case "TransformDrawToRadiantBlade":
                    CardPile draw = PileType.Draw.GetPile(Owner);
                    int count = Math.Min((int)amount, draw.Cards.Count(card => card.IsTransformable));
                    if (count == 0) break;
                    var transforms = (await CardSelectCmd.FromCombatPile(context, draw, Owner,
                        new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count),
                        card => card.IsTransformable)).ToArray();
                    foreach (CardModel card in transforms)
                    {
                        CardModel replacement = effect.Kind == "TransformDrawToDodge"
                            ? CombatState.CreateCard<DuchessDodge>(Owner)
                            : CombatState.CreateCard<DuchessRadiantBlade>(Owner);
                        if (IsUpgraded && Spec.UpgradeTokens) CardCmd.Upgrade(replacement);
                        if (replacement is DuchessRadiantBlade
                            && Owner.Creature.GetPower<DuchessRadiantBladeGrowthPower>() is { } growthPower)
                            replacement.DynamicVars.Damage.BaseValue += growthPower.TotalGrowth;
                        await CardCmd.Transform(card, replacement);
                    }
                    break;
                case "ShuffleAoeDamage": break;
                case "ReturnSelfToDrawTop":
                    await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Top, this);
                    break;
                case "ReturnHandDamageBoost":
                    PendingGlintstoneDamage += decimal.ToInt32(amount);
                    DynamicVars.Damage.BaseValue += amount;
                    await CardPileCmd.Add(this, PileType.Hand, CardPilePosition.Top, this);
                    break;
                default: throw new InvalidOperationException($"Unknown Duchess effect: {effect.Kind}");
            }
        }

        // One reward per successful card's Moment condition, not per effect line.
        bool triggeredMoment = Spec.Effects.Any(e => e.Condition.StartsWith("moment", StringComparison.Ordinal)
            && conditions.TryGetValue(e.Condition, out bool active) && active)
            || conditions["moment"] && (Spec.MomentCostReduction > 0 || Spec.RestageDivisor > 0);
        if (triggeredMoment && Owner.Creature.GetPower<DuchessEternalFormPower>() is { } eternalForm)
            await eternalForm.OnMomentEffect();
        if (triggeredMoment)
            foreach (NightMustStay.Core.Models.Relics.DuchessCarianBadge badge in
                     Owner.Relics.OfType<NightMustStay.Core.Models.Relics.DuchessCarianBadge>().ToArray())
                await badge.OnMomentEffect(context);


        if (Spec.ShuffleSelf)
            await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Random, this);
    }

    private async Task TransformStrikeInDraw(PlayerChoiceContext context)
    {
        if (!PileType.Draw.GetPile(Owner).Cards.Any(card => card is DuchessStrike
                && card.IsTransformable && card.DeckVersion?.Pile?.Type == PileType.Deck))
            return;
        CardModel selected = (await CardSelectCmd.FromCombatPile(
            context, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            card => card is DuchessStrike && card.IsTransformable
                && card.DeckVersion?.Pile?.Type == PileType.Deck)).FirstOrDefault();
        if (selected?.DeckVersion is not CardModel deckStrike) return;

        DuchessCarianSlicer deckSlicer = Owner.RunState.CreateCard<DuchessCarianSlicer>(Owner);
        DuchessCarianSlicer combatSlicer = CombatState.CreateCard<DuchessCarianSlicer>(Owner);
        CarryStrikeUpgrade(selected, deckSlicer, combatSlicer);

        var deckResult = await CardCmd.Transform(deckStrike, deckSlicer, CardPreviewStyle.None);
        if (deckResult == null) return;
        if (deckResult.Value.cardAdded.IsUpgraded && !combatSlicer.IsUpgraded)
            CardCmd.Upgrade(combatSlicer);
        combatSlicer.DeckVersion = deckResult.Value.cardAdded;
        await CardCmd.Transform(selected, combatSlicer);
    }

    private async Task ChooseDrawToTop(PlayerChoiceContext context)
    {
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count == 0) return;
        CardModel selected = (await CardSelectCmd.FromCombatPile(
            context, drawPile, Owner,
            new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_DRAW_TO_TOP"), 1)))
            .FirstOrDefault();
        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top, this);
    }

    private async Task HandToDrawTopBlock(PlayerChoiceContext context, decimal blockPerCard)
    {
        CardPile hand = PileType.Hand.GetPile(Owner);
        if (hand.Cards.Count == 0) return;
        CardModel[] selected = (await CardSelectCmd.FromCombatPile(
            context, hand, Owner,
            new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_HAND_TO_TOP"), 0, hand.Cards.Count),
            card => card != this)).ToArray();
        foreach (CardModel card in selected)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, this);
            await CreatureCmd.GainBlock(Owner.Creature, blockPerCard, ValueProp.Unpowered, null);
        }
    }

    private static void CarryStrikeUpgrade(CardModel selected, CardModel deckSlicer, CardModel combatSlicer)
    {
        if (!selected.IsUpgraded) return;
        CardCmd.Upgrade(deckSlicer);
        CardCmd.Upgrade(combatSlicer);
    }

    private async Task AddDodges(MegaCrit.Sts2.Core.Entities.Players.Player player, decimal amount, PileType destination,
        CardPilePosition? position = null)
    {
        for (int i = 0; i < amount; i++)
        {
            DuchessDodge dodge = CombatState.CreateCard<DuchessDodge>(player);
            if (IsUpgraded && Spec.UpgradeTokens) CardCmd.Upgrade(dodge);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(dodge, destination, player,
                position ?? (destination == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Top));
            CardCmd.PreviewCardPileAdd(result);
        }
    }

    private async Task AllyDodgeDrawX(PlayerChoiceContext context)
    {
        int count = ResolveEnergyXValue() + (IsUpgraded && Spec.UpgradeX ? 1 : 0);
        foreach (var ally in CombatState.Players.Where(p => p != Owner && p.Creature.IsAlive))
        {
            await AddDodges(ally, count, PileType.Draw);
            await CardPileCmd.Draw(context, count, ally);
        }
    }

    private async Task DrawReactionFromPile(PlayerChoiceContext context)
    {
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        CardModel[] originalOrder = drawPile.Cards.ToArray();
        CardModel[] choices = originalOrder
            .Where(card => card is DuchessCard { HasReaction: true }).ToArray();
        if (choices.Length == 0) return;
        CardModel selected = Owner.RunState.Rng.Niche.NextItem(choices);
        // Native Draw owns prevention, hand limits, history and all AfterCardDrawn
        // hooks (Reaction, Lightning Nerves, Composure, etc.). This is not a hand draw.
        drawPile.MoveToTopInternal(selected);
        CardModel drawn = await CardPileCmd.Draw(context, Owner);
        if (drawn == null)
        {
            // A prevented draw must not silently change the draw-pile order.
            foreach (CardModel card in originalOrder.Reverse())
                if (card.Pile == drawPile) drawPile.MoveToTopInternal(card);
        }
    }

    private async Task AddRadiantBlades(MegaCrit.Sts2.Core.Entities.Players.Player player, decimal amount, PileType destination)
    {
        for (int i = 0; i < amount; i++)
        {
            DuchessRadiantBlade blade = CombatState.CreateCard<DuchessRadiantBlade>(player);
            if (IsUpgraded && Spec.UpgradeTokens) CardCmd.Upgrade(blade);
            if (player.Creature.HasPower<DuchessRadiantBladeGrowthPower>())
                blade.DynamicVars.Damage.BaseValue += player.Creature.GetPower<DuchessRadiantBladeGrowthPower>().TotalGrowth;
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(blade, destination, player,
                destination == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Top);
            CardCmd.PreviewCardPileAdd(result);
        }
    }

    private async Task AddInstinctRadiantBladesToDraw(decimal amount)
    {
        for (int i = 0; i < amount; i++)
        {
            DuchessRadiantBlade blade = CombatState.CreateCard<DuchessRadiantBlade>(Owner);
            if (IsUpgraded && Spec.UpgradeTokens) CardCmd.Upgrade(blade);
            if (Owner.Creature.HasPower<DuchessRadiantBladeGrowthPower>())
                blade.DynamicVars.Damage.BaseValue += Owner.Creature.GetPower<DuchessRadiantBladeGrowthPower>().TotalGrowth;
            CardCmd.Enchant<Instinct>(blade, 1m);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                blade, PileType.Draw, Owner, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(result);
        }
    }

    private async Task ShuffleSelected(PlayerChoiceContext context, CardPile pile, int requested)
    {
        int count = Math.Min(requested, pile.Cards.Count);
        if (count <= 0) return;
        var prefs = new CardSelectorPrefs(new LocString("cards", "DUCHESS_SELECT_TO_SHUFFLE"), count);
        CardModel[] selected = (await CardSelectCmd.FromCombatPile(context, pile, Owner, prefs)).ToArray();
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Random, this);
    }

    private async Task Apply<T>(PlayerChoiceContext context, Creature target, decimal amount) where T : PowerModel, new() =>
        await PowerCmd.Apply<T>(context, target, amount, Owner.Creature, this);
}
