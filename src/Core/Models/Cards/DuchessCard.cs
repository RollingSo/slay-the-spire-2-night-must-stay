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
    protected override bool ShouldGlowGoldInternal => IsMomentActive;

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
                    yield return new ExtraDamageVar(1m);
                    yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
                        static (card, _) => DuchessMomentPower.Current(card.Owner));
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
                    "Energy" or "NextTurnEnergy" or "FutureMomentEnergy" =>
                        new EnergyVar(effect.Kind, (int)effect.Amount),
                    "TurnStartSwap" => new PowerVar<DuchessTurnStartSwapPower>(effect.Kind, effect.Amount),
                    "Concealment" => new PowerVar<DuchessConcealmentPower>(effect.Kind, effect.Amount),
                    "ReactionDrawBlock" => new PowerVar<DuchessReactionDrawBlockPower>(effect.Kind, effect.Amount),
                    "RadiantBladeGrowth" => new PowerVar<DuchessRadiantBladeGrowthPower>(effect.Kind, effect.Amount),
                    "EndTurnDodge" => new PowerVar<DuchessEndTurnDodgePower>(effect.Kind, effect.Amount),
                    "MomentFiveFirstEnergy" => new PowerVar<DuchessBeatPower>(effect.Kind, effect.Amount),
                    "MomentFiveBlock" => new PowerVar<DuchessMomentFiveBlockPower>(effect.Kind, effect.Amount),
                    "RestageEndTurnAoe" => new PowerVar<DuchessEternalRestagePower>(effect.Kind, effect.Amount),
                    "ReactionBlock" => new PowerVar<DuchessReactionBlockPower>(effect.Kind, effect.Amount),
                    "ReactionDraw" => new PowerVar<DuchessReactionDrawPower>(effect.Kind, effect.Amount),
                    "MomentFiveDraw" => new PowerVar<DuchessMomentFiveDrawPower>(effect.Kind, effect.Amount),
                    "DodgeMoment" => new PowerVar<DuchessDodgeMomentPower>(effect.Kind, effect.Amount),
                    "ShuffleBlock" => new PowerVar<DuchessShuffleBlockPower>(effect.Kind, effect.Amount),
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
                if (effect.Kind is "DodgeToDraw" or "DodgeToHand" or "AllyDodge"
                    or "EndTurnDodge" or "AllyDodgeDrawX")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessDodge>(IsUpgraded && Spec.UpgradeTokens))
                        yield return tip;
                if (effect.Kind is "RadiantBladeToDraw" or "RadiantBladeToHand" or "RadiantBladeTurns"
                    or "InstinctRadiantBladesToDraw" or "FullBlockRadiantBlade" or "RadiantBladeGrowth")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessRadiantBlade>(
                                 effect.Kind is "RadiantBladeToDraw" or "RadiantBladeToHand" or "InstinctRadiantBladesToDraw" && IsUpgraded && Spec.UpgradeTokens))
                        yield return tip;
                if (effect.Kind == "InstinctRadiantBladesToDraw")
                    foreach (IHoverTip tip in HoverTipFactory.FromEnchantment<Instinct>())
                        yield return tip;
                if (effect.Kind is "Weak" or "WeakAll") yield return HoverTipFactory.FromPower<WeakPower>();
                if (effect.Kind == "Vulnerable") yield return HoverTipFactory.FromPower<VulnerablePower>();
                if (effect.Kind == "Strength") yield return HoverTipFactory.FromPower<StrengthPower>();
                if (effect.Kind == "TemporaryStrength") yield return HoverTipFactory.FromPower<StrengthPower>();
                if (effect.Kind == "Intangible") yield return HoverTipFactory.FromPower<IntangiblePower>();
                if (effect.Kind == "AllyIntangible") yield return HoverTipFactory.FromPower<IntangiblePower>();
                if (effect.Kind == "TurnStartSwap") yield return HoverTipFactory.FromPower<DuchessTurnStartSwapPower>();
                if (effect.Kind == "Concealment" || effect.Condition == "concealed" || Spec.ConcealedTripleDamage)
                    yield return HoverTipFactory.FromPower<DuchessConcealmentPower>();
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
            var falling = Spec.Effects.FirstOrDefault(effect => effect.Kind == "ShuffleRandomDamage");
            if (falling != null && CombatState.HittableEnemies.Any(enemy => enemy.IsAlive))
                await DamageCmd.Attack(DynamicVars["ShuffleRandomDamage"].BaseValue)
                    .CompatFromCard(this).TargetingRandomOpponents(CombatState)
                    .Execute(new BlockingPlayerChoiceContext());
        }
        if (card == this && this is DuchessCarianPiercer && oldPileType == PileType.Draw
            && card.Pile?.Type == PileType.Hand)
        {
            int boost = decimal.ToInt32(DynamicVars["DrawReactionDamageBoost"].BaseValue);
            PendingPiercerDamage += boost;
            DynamicVars.Damage.BaseValue += boost;
        }
        if (!Spec.Reaction || card != this || oldPileType != PileType.Draw
            || card.Pile?.Type != PileType.Hand || Owner?.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
            return;
        EnergyCost.AddUntilPlayed(-1, true);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
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
                : effect.Kind == "MomentDamage" ? DynamicVars.CalculatedDamage.Calculate(play.Target)
                : DynamicVars.TryGetValue(key, out DynamicVar variable) ? variable.BaseValue : effect.Amount;
            Creature[] enemies = Spec.All
                ? CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray()
                : play.Target is { IsAlive: true } target && target.Side != Owner.Creature.Side
                    ? new[] { target } : Array.Empty<Creature>();

            switch (effect.Kind)
            {
                case "Damage":
                    for (int i = 0; i < (IsUpgraded && Spec.UpgradeHits > 0 ? Spec.UpgradeHits : Spec.Hits); i++)
                        foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                            await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(enemy)
                                .WithHitVfxNode(NightMustStay.Core.Nodes.Vfx.DuchessSlashVfx.Create).Execute(context);
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
                    foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                        await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(enemy)
                            .WithHitVfxNode(NightMustStay.Core.Nodes.Vfx.DuchessSlashVfx.Create).Execute(context);
                    break;
                case "AoeDamage":
                    await DamageCmd.Attack(amount).CompatFromCard(this)
                        .TargetingAllOpponents(CombatState).Execute(context);
                    break;
                case "ExtraDamage":
                    foreach (Creature enemy in enemies.Where(e => e.IsAlive))
                        await DamageCmd.Attack(amount).CompatFromCard(this).Targeting(enemy)
                            .WithHitVfxNode(NightMustStay.Core.Nodes.Vfx.DuchessSlashVfx.Create).Execute(context);
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
                    await Apply<StrengthPower>(context, Owner.Creature, amount);
                    await Apply<DuchessTemporaryStrengthDownPower>(context, Owner.Creature, amount);
                    break;
                case "Intangible": await Apply<IntangiblePower>(context, Owner.Creature, amount); break;
                case "AllyIntangible":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await Apply<IntangiblePower>(context, ally.Creature, amount);
                    break;
                case "Dexterity": await Apply<DexterityPower>(context, Owner.Creature, amount); break;
                case "DodgeToDraw": await AddDodges(Owner, amount, PileType.Draw); break;
                case "DodgeToHand": await AddDodges(Owner, amount, PileType.Hand); break;
                case "AllyDodge":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await AddDodges(ally, amount, PileType.Hand);
                    break;
                case "AdvanceMoment": await DuchessMomentPower.Advance(context, Owner.Creature, (int)amount, this); break;
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
                case "DrawReactionFromPile": await DrawReactionFromPile(); break;
                case "MomentTwelveEndTurn": break;
                case "ConcealedBonusDamage": break;
                case "MomentBonusDamage": break;
                case "DrawReactionDamageBoost": break;
                case "HandToDrawTopBlock": await HandToDrawTopBlock(context, amount); break;
                case "DrawUntilReaction":
                    while (PileType.Hand.GetPile(Owner).Cards.Count < CardPile.MaxCardsInHand)
                    {
                        CardModel drawn = await CardPileCmd.Draw(context, Owner);
                        if (drawn == null || drawn is DuchessCard { HasReaction: true }) break;
                    }
                    break;
                case "ReactionBlock": await Apply<DuchessReactionBlockPower>(context, Owner.Creature, amount); break;
                case "ReactionDraw": await Apply<DuchessReactionDrawPower>(context, Owner.Creature, amount); break;
                case "TransformStrike": await TransformStrikeInDraw(context); break;
                case "ChooseDrawToTop": await ChooseDrawToTop(context); break;
                case "EndTurnMomentBlock": await Apply<DuchessEndTurnMomentBlockPower>(context, Owner.Creature, amount); break;
                case "MomentFiveDraw": await Apply<DuchessMomentFiveDrawPower>(context, Owner.Creature, amount); break;
                case "DodgeMoment": await Apply<DuchessDodgeMomentPower>(context, Owner.Creature, amount); break;
                case "ShuffleBlock": await Apply<DuchessShuffleBlockPower>(context, Owner.Creature, amount); break;
                case "RestageAoe":
                    decimal repeats = decimal.Floor(DuchessMomentPower.DamageDealtThisTurn(this) / amount);
                    if (repeats > 0)
                        await DamageCmd.Attack(repeats).CompatFromCard(this).TargetingAllOpponents(CombatState).Execute(context);
                    break;
                case "NextTurnEnergy": await Apply<DuchessNextTurnEnergyPower>(context, Owner.Creature, amount); break;
                case "NextTurnDraw": await Apply<DuchessNextTurnDrawPower>(context, Owner.Creature, amount); break;
                case "NextTurnEnergyAndDraw":
                    await Apply<DuchessNextTurnEnergyPower>(context, Owner.Creature, 1m);
                    await Apply<DuchessNextTurnDrawPower>(context, Owner.Creature, amount);
                    break;
                case "FutureMomentEnergy":
                    if (DuchessMomentPower.Current(Owner) == 6)
                        await PlayerCmd.GainEnergy(amount, Owner);
                    else
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
                case "ShuffleGrowth": break;
                case "ShuffleRandomDamage": break;
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

    private async Task AddDodges(MegaCrit.Sts2.Core.Entities.Players.Player player, decimal amount, PileType destination)
    {
        for (int i = 0; i < amount; i++)
        {
            DuchessDodge dodge = CombatState.CreateCard<DuchessDodge>(player);
            if (IsUpgraded && Spec.UpgradeTokens) CardCmd.Upgrade(dodge);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(dodge, destination, player,
                destination == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Top);
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

    private async Task DrawReactionFromPile()
    {
        CardModel[] choices = PileType.Draw.GetPile(Owner).Cards
            .Where(card => card is DuchessCard { HasReaction: true }).ToArray();
        if (choices.Length == 0) return;
        CardModel selected = Owner.RunState.Rng.Niche.NextItem(choices);
        await CardPileCmd.Add(selected, PileType.Hand, CardPilePosition.Top, this);
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
