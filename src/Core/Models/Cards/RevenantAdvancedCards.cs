using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Cards;

public interface IRevenantChargeCard
{
    bool IsChargeComplete { get; }
    Task CompleteCharge(PlayerChoiceContext context);
}

/// <summary>Shared, deliberately small helpers for the Revenant card set.</summary>
internal static class RevenantCardHelpers
{
    public static Creature Family(CardModel card) => card.Owner?.Osty;

    public static bool WasMovedFromDiscardToHand(CardModel card, PileType oldPileType) =>
        oldPileType == PileType.Discard && card.Pile?.Type == PileType.Hand;

    public static async Task HealFamily(CardModel card, decimal amount)
    {
        Creature family = Family(card);
        if (family is { IsAlive: true })
            await CreatureCmd.Heal(family, amount);
    }

    public static async Task DamageRandom(CardModel card, PlayerChoiceContext context, decimal amount, int hits = 1)
    {
        Creature[] enemies = card.CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray();
        if (enemies.Length == 0) return;
        Creature target = card.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        for (int i = 0; i < hits && target.IsAlive; i++)
            await CreatureCmd.Damage(context, target, amount, ValueProp.Move, card.Owner.Creature, card);
    }

    public static async Task DamageRandomEachHit(
        CardModel card,
        PlayerChoiceContext context,
        decimal amount,
        int hits)
    {
        for (int i = 0; i < hits; i++)
        {
            Creature[] enemies = card.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
            if (enemies.Length == 0)
                return;
            Creature target = card.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            await CreatureCmd.Damage(
                context,
                target,
                amount,
                ValueProp.Move,
                card.Owner.Creature,
                card);
        }
    }

    public static async Task DamageFamily(CardModel card, PlayerChoiceContext context, decimal amount)
    {
        Creature family = Family(card);
        if (family is not { IsAlive: true } || amount <= 0m) return;
        await CreatureCmd.Damage(
            context,
            family,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            card.Owner.Creature,
            card);
    }

    public static async Task DiscardFromDrawTopWithShuffle(
        CardModel card,
        PlayerChoiceContext context,
        int count)
    {
        if (count <= 0)
            return;

        CardPile draw = PileType.Draw.GetPile(card.Owner);
        CardPile discard = PileType.Discard.GetPile(card.Owner);
        if (draw.Cards.Count < count && discard.Cards.Count > 0)
            await CardPileCmd.Shuffle(context, card.Owner);

        for (int i = 0; i < count && draw.Cards.Count > 0; i++)
            await CardPileCmd.Add(draw.Cards[0], PileType.Discard);
    }

    public static async Task<IReadOnlyList<CardModel>> AddFromDiscard(CardModel card, PlayerChoiceContext context, int count, bool zeroCost)
    {
        CardPile discard = PileType.Discard.GetPile(card.Owner);
        if (discard.Cards.Count == 0) return Array.Empty<CardModel>();
        CardModel[] selected = (await CardSelectCmd.FromCombatPile(
            context, discard, card.Owner,
            new CardSelectorPrefs(new LocString("cards", "REVENANT_RECOVER_CARDS"), Math.Min(count, discard.Cards.Count)))).ToArray();
        foreach (CardModel selectedCard in selected)
        {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
            if (zeroCost) selectedCard.EnergyCost.SetThisTurn(0, true);
        }
        return selected;
    }

    public static Task AutoPlayWhenRecovered(CardModel card, PileType oldPileType)
    {
        if (!WasMovedFromDiscardToHand(card, oldPileType))
            return Task.CompletedTask;
        return CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, null);
    }

    public static async Task AddFromDraw(CardModel card, PlayerChoiceContext context, int count)
    {
        CardPile draw = PileType.Draw.GetPile(card.Owner);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            context, draw, card.Owner,
            new CardSelectorPrefs(new LocString("cards", "REVENANT_SELECT_CARD"), Math.Min(count, draw.Cards.Count)),
            c => c.Type == CardType.Attack);
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    public static async Task ChargeResonance(CardModel card, PlayerChoiceContext context)
    {
        await RevenantSummonManager.For(card.Owner).TriggerResonance(context);
    }

    public static void AddChargeStateDescription(
        CardModel card,
        LocString description,
        bool chargeComplete,
        Action<LocString> configure = null)
    {
        string suffix = chargeComplete ? ".chargedDescription" : ".unchargedDescription";
        var stateDescription = new LocString("cards", card.Id.Entry + suffix);
        card.DynamicVars.AddTo(stateDescription);
        configure?.Invoke(stateDescription);
        description.Add("ChargeStateText", stateDescription.GetFormattedText());
    }
}

public sealed class EmergencyRestore : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(4m, ValueProp.Move), new DynamicVar("Heal", 6m)
    };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/emergency_restore.png";
    // Use the hybrid targeting path so solo combat can select either the
    // Revenant herself or her Osty-backed family creature.
    public EmergencyRestore() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == Owner.Creature)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        else if (cardPlay.Target == Owner.Osty)
            await RevenantCardHelpers.HealFamily(this, DynamicVars["Heal"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}

public sealed class PreciseLightningStrike : CardModel
{
    private bool _recoveredThisTurn;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
    };
    public override string PortraitPath => "res://revenant_assets/cards/precise_lightning_strike.png";
    public PreciseLightningStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal damage = _recoveredThisTurn
            ? DynamicVars.Damage.BaseValue * 2m
            : DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage).FromCard(this).Targeting(cardPlay.Target).Execute(context);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            _recoveredThisTurn = true;
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Creature.Side)
            _recoveredThisTurn = false;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class ThreefoldHalo : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(12m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };
    public override string PortraitPath => "res://revenant_assets/cards/threefold_halo.png";
    public ThreefoldHalo() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState).Execute(context); }
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        await PowerCmd.Apply<HaloReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    public void ReduceCostForCurrentCombat() => EnergyCost.AddThisCombat(-1, true);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class AncientDragonLightning : CardModel
{
    private bool _recoveredThisTurn;
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/ancient_dragon_lightning.png";
    public AncientDragonLightning() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        int hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0) + (_recoveredThisTurn ? 2 : 0);
        await RevenantCardHelpers.DamageRandomEachHit(
            this,
            context,
            DynamicVars.Damage.BaseValue,
            hits);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            _recoveredThisTurn = true;
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Creature.Side)
            _recoveredThisTurn = false;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() { }
}

public sealed class LansseaxBlade : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(63m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/lansseax_blade.png";
    public LansseaxBlade() : base(5, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    { ArgumentNullException.ThrowIfNull(cardPlay.Target); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).Execute(context); }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.AddThisCombat(-1, true);
        return Task.CompletedTask;
    }
}

public sealed class LightningStrike : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(12m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/lightning_strike.png";
    public LightningStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    { ArgumentNullException.ThrowIfNull(cardPlay.Target); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).Execute(context); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            EnergyCost.SetThisTurn(0, true);
        return Task.CompletedTask;
    }
}

public sealed class AncientDragonSpear : CardModel
{
    private bool _recoveredThisTurn;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
        new PowerVar<VulnerablePower>("Vulnerable", 2m),
    };
    public override string PortraitPath => "res://revenant_assets/cards/ancient_dragon_spear.png";
    public AncientDragonSpear() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(context);
        if (_recoveredThisTurn && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(
                context,
                cardPlay.Target,
                DynamicVars["Vulnerable"].BaseValue,
                Owner.Creature,
                this);
        }
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card == this && RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            _recoveredThisTurn = true;
        return Task.CompletedTask;
    }
    public override Task AfterSideTurnEnd(
        PlayerChoiceContext context,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Creature.Side)
            _recoveredThisTurn = false;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}

public sealed class Recover : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(6m, ValueProp.Move), new DynamicVar("Heal", 4m) };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/recover.png";
    public Recover() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        RevenantSummonManager manager = RevenantSummonManager.For(Owner);
        if (manager.CurrentFamilyCreature is { IsAlive: true } family)
            await CreatureCmd.Heal(family, DynamicVars["Heal"].BaseValue);
        foreach (RevenantNecro necro in manager.GetLivingNecros())
            await CreatureCmd.Heal(necro.Creature, DynamicVars["Heal"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}

public sealed class FlannSaxLightningSpear : CardModel
{
    private object _combatIdentity;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(5m, ValueProp.Move), new RepeatVar(3) };
    public override string PortraitPath => "res://revenant_assets/cards/flannsax_lightning_spear.png";
    public FlannSaxLightningSpear() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    { EnsureCombatValue(); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState).WithHitCount(DynamicVars.Repeat.IntValue).Execute(context); }
    private void EnsureCombatValue() { if (ReferenceEquals(_combatIdentity, CombatState)) return; _combatIdentity = CombatState; DynamicVars.Damage.BaseValue = 5m; }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card != this) return Task.CompletedTask;
        EnsureCombatValue();
        if (RevenantCardHelpers.WasMovedFromDiscardToHand(card, oldPileType))
            DynamicVars.Damage.BaseValue += 2m;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}

public sealed class BeastClaw : CardModel, IRevenantChargeCard
{
    private int _chargeCount;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new DynamicVar("ChargeDamage", 8m), new BoolVar("Ready") };
    public override string PortraitPath => "res://revenant_assets/cards/beast_claw.png";
    public bool IsChargeComplete => _chargeCount > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };
    public BeastClaw() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete, state =>
            state.Add("ChargedDamage", DynamicVars.Damage.BaseValue + DynamicVars["ChargeDamage"].BaseValue));
    private BeastClaw CreateOppositeChargePreview()
    {
        var preview = (BeastClaw)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }
    private void SetChargePreviewState(bool complete)
    {
        _chargeCount = complete ? 1 : 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        bool wasCharged = _chargeCount > 0;
        decimal damage = DynamicVars.Damage.BaseValue + DynamicVars["ChargeDamage"].BaseValue * _chargeCount;
        _chargeCount = 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;
        await DamageCmd.Attack(damage).FromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        if (wasCharged) await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
        if (wasCharged) await RevenantCardHelpers.ChargeResonance(this, context);
    }
    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (IsChargeComplete) return;
        _chargeCount = 1;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted();
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(0m); DynamicVars["ChargeDamage"].UpgradeValueBy(6m); }
}

public sealed class DeathLightning : CardModel, IRevenantChargeCard
{
    private int _chargeCount;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(5m, ValueProp.Move), new RepeatVar(4), new DynamicVar("ChargeHits", 5m), new BoolVar("Ready") };
    public override string PortraitPath => "res://revenant_assets/cards/death_lightning.png";
    public bool IsChargeComplete => _chargeCount > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };
    public DeathLightning() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete, state =>
            state.Add("ChargedHits", DynamicVars.Repeat.IntValue + DynamicVars["ChargeHits"].IntValue));
    private DeathLightning CreateOppositeChargePreview()
    {
        var preview = (DeathLightning)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }
    private void SetChargePreviewState(bool complete)
    {
        _chargeCount = complete ? 1 : 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        bool wasCharged = _chargeCount > 0;
        int hits = DynamicVars.Repeat.IntValue + DynamicVars["ChargeHits"].IntValue * _chargeCount;
        _chargeCount = 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target).WithHitCount(hits).Execute(context);
        if (wasCharged) await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
    }
    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (IsChargeComplete) return;
        _chargeCount = 1;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted();
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class SpaceRendingFrenzy : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(16m, ValueProp.Move), new DynamicVar("FamilyDamage", 5m) };
    public override string PortraitPath => "res://revenant_assets/cards/space_rending_frenzy.png";
    protected override bool IsPlayable => RevenantSummonManager.For(Owner).HasLivingFamily;
    public SpaceRendingFrenzy() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) { await RevenantCardHelpers.DamageFamily(this, context, DynamicVars["FamilyDamage"].BaseValue); Creature[] enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray(); if (enemies.Length == 0) return; Creature target = Owner.RunState.Rng.CombatTargets.NextItem(enemies); await CreatureCmd.Damage(context, target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

public sealed class WhiteShadowLure : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Prevent", 1m) };
    public override string PortraitPath => "res://revenant_assets/cards/white_shadow_lure.png";
    public WhiteShadowLure() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        RevenantSummonManager manager = RevenantSummonManager.For(Owner);
        if (manager.CurrentFamilyCreature is { IsAlive: true } family)
            await PowerCmd.Apply<BufferPower>(context, family, DynamicVars["Prevent"].BaseValue, Owner.Creature, this);
        foreach (RevenantNecro necro in manager.GetLivingNecros())
            await PowerCmd.Apply<BufferPower>(context, necro.Creature, DynamicVars["Prevent"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Prevent"].UpgradeValueBy(1m);
}

public sealed class Soulguard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(3m, ValueProp.Move) };
    public override string PortraitPath => "res://revenant_assets/cards/soulguard.png";
    public Soulguard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<SoulguardPower>(context, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}

public sealed class LightningSpear : CardModel, IRevenantChargeCard
{
    private int _chargeCount;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new DynamicVar("ChargeDamage", 14m), new BoolVar("Ready") };
    public override string PortraitPath => "res://revenant_assets/cards/lightning_spear.png";
    public bool IsChargeComplete => _chargeCount > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };
    public LightningSpear() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete);
    private LightningSpear CreateOppositeChargePreview()
    {
        var preview = (LightningSpear)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }
    private void SetChargePreviewState(bool complete)
    {
        _chargeCount = complete ? 1 : 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            return;
        }

        bool wasCharged = _chargeCount > 0;
        await CreatureCmd.Damage(context, cardPlay.Target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this);
        for (int i = 0; i < _chargeCount && cardPlay.Target.IsAlive; i++)
            await CreatureCmd.Damage(context, cardPlay.Target, DynamicVars["ChargeDamage"].BaseValue, ValueProp.Move, Owner.Creature, this);
        _chargeCount = 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;
        if (wasCharged) await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
    }
    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (IsChargeComplete) return;
        _chargeCount = 1;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted();
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class SpiritForm : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/spirit_form.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? new[] { CardKeyword.Retain } : Array.Empty<CardKeyword>();
    public SpiritForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<SpiritFormPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class UnbearableFrenzy : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(4m, ValueProp.Move), new DynamicVar("FamilyDamage", 8m), new RepeatVar(6) };
    public override string PortraitPath => "res://revenant_assets/cards/unbearable_frenzy.png";
    protected override bool IsPlayable => RevenantSummonManager.For(Owner).HasLivingFamily;
    public UnbearableFrenzy() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) { await RevenantCardHelpers.DamageFamily(this, context, DynamicVars["FamilyDamage"].BaseValue); Creature[] enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray(); if (enemies.Length == 0) return; Creature target = Owner.RunState.Rng.CombatTargets.NextItem(enemies); await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).WithHitCount(DynamicVars.Repeat.IntValue).Execute(context); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}

public sealed class Beaststone : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(5m, ValueProp.Move), new DynamicVar("Strength", 1m) };
    public override string PortraitPath => "res://revenant_assets/cards/beaststone.png";
    public Beaststone() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.Damage(context, cardPlay.Target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this);
        Creature family = Owner.Osty;
        if (family is { IsAlive: true })
            await PowerCmd.Apply<StrengthPower>(context, family, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Strength"].UpgradeValueBy(1m); }
}

public sealed class RadagonHalo : CardModel
{
    private object _combatIdentity;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(12m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };
    public override string PortraitPath => "res://revenant_assets/cards/radagon_halo.png";
    public RadagonHalo() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) { EnsureCombatValue(); ArgumentNullException.ThrowIfNull(cardPlay.Target); await CreatureCmd.Damage(context, cardPlay.Target, DynamicVars.Damage.BaseValue, ValueProp.Move, Owner.Creature, this); await PowerCmd.Apply<HaloReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this); }
    private void EnsureCombatValue() { if (ReferenceEquals(_combatIdentity, CombatState)) return; _combatIdentity = CombatState; DynamicVars.Damage.BaseValue = IsUpgraded ? 15m : 12m; }
    public void DoubleDamageForCurrentCombat() { EnsureCombatValue(); DynamicVars.Damage.BaseValue *= 2m; }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class SoulSummon : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/soul_summon.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    public SoulSummon() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardPile discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            context, discard, Owner,
            new CardSelectorPrefs(new LocString("cards", "REVENANT_RECOVER_CARDS"), Math.Min(2, discard.Cards.Count)));
        foreach (CardModel selectedCard in selected)
        {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
            selectedCard.EnergyCost.SetUntilPlayed(Math.Max(0, selectedCard.EnergyCost.GetResolved() - 1));
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class GraveRob : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };
    public override string PortraitPath => "res://revenant_assets/cards/grave_rob.png";
    public GraveRob() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await RevenantCardHelpers.DiscardFromDrawTopWithShuffle(this, context, DynamicVars.Cards.IntValue);
        await RevenantCardHelpers.AddFromDiscard(this, context, 1, false);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}

public sealed class GreaterRecover : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(12m, ValueProp.Move), new DynamicVar("Heal", 12m) };
    public override bool GainsBlock => true;
    public override string PortraitPath => "res://revenant_assets/cards/greater_recover.png";
    public GreaterRecover() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) { await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay); await RevenantCardHelpers.HealFamily(this, DynamicVars["Heal"].BaseValue); }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public sealed class AncientDragonFaith : CardModel
{
    public override string PortraitPath => "res://revenant_assets/cards/ancient_dragon_faith.png";
    public AncientDragonFaith() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<AncientDragonFaithPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class BeastClawMark : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Strength", 2m) };
    public override string PortraitPath => "res://revenant_assets/cards/beast_claw_mark.png";
    public BeastClawMark() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<BeastClawMarkPower>(context, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}

public sealed class GoldenOrder : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(CardKeyword.Ethereal) };
    public override string PortraitPath => "res://revenant_assets/cards/golden_order.png";
    public GoldenOrder() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<GoldenOrderPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class SpiritLink : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("MaxHp", 4m) };
    public override string PortraitPath => "res://revenant_assets/cards/spirit_link.png";
    public SpiritLink() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<SpiritLinkPower>(context, Owner.Creature, DynamicVars["MaxHp"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["MaxHp"].UpgradeValueBy(2m);
}

public sealed class BlessingOfGrace : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Heal", 4m) };
    public override string PortraitPath => "res://revenant_assets/cards/blessing_of_grace.png";
    public BlessingOfGrace() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) => PowerCmd.Apply<BlessingOfGracePower>(context, Owner.Creature, DynamicVars["Heal"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class GurranqBeastClaw : CardModel, IRevenantChargeCard
{
    private int _chargeCount;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(13m, ValueProp.Move), new DynamicVar("ChargeDamage", 10m), new BoolVar("Ready") };
    public override string PortraitPath => "res://revenant_assets/cards/gurranq_beast_claw.png";
    public bool IsChargeComplete => _chargeCount > 0;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        new CardHoverTip(CreateOppositeChargePreview()),
    };
    public GurranqBeastClaw() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override void AddExtraArgsToDescription(LocString description) =>
        RevenantCardHelpers.AddChargeStateDescription(this, description, IsChargeComplete, state =>
            state.Add("ChargedDamage", DynamicVars.Damage.BaseValue + DynamicVars["ChargeDamage"].BaseValue));
    private GurranqBeastClaw CreateOppositeChargePreview()
    {
        var preview = (GurranqBeastClaw)MutableClone();
        preview.SetChargePreviewState(!IsChargeComplete);
        return preview;
    }
    private void SetChargePreviewState(bool complete)
    {
        _chargeCount = complete ? 1 : 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = complete;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target == Owner.Creature)
        {
            await CompleteCharge(context);
            await RevenantCardHelpers.ChargeResonance(this, context);
            return;
        }

        bool wasCharged = _chargeCount > 0;
        decimal damage = DynamicVars.Damage.BaseValue + DynamicVars["ChargeDamage"].BaseValue * _chargeCount;
        _chargeCount = 0;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = false;
        await DamageCmd.Attack(damage).FromCard(this).TargetingAllOpponents(CombatState).Execute(context);
        if (wasCharged) await RevenantSummonManager.For(Owner).NotifyChargedCardPlayed(context);
        await RevenantCardHelpers.ChargeResonance(this, context);
    }
    public async Task CompleteCharge(PlayerChoiceContext context)
    {
        if (IsChargeComplete) return;
        _chargeCount = 1;
        ((BoolVar)DynamicVars["Ready"]).BoolVal = true;
        await RevenantSummonManager.For(Owner).NotifyChargeCompleted();
        await PowerCmd.Apply<ChargeReturnPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["ChargeDamage"].UpgradeValueBy(5m);
}
