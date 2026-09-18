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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Cards;

public record DuchessEffect(string Kind, decimal Amount, decimal Upgraded, string Condition = "");
public record DuchessCardSpec(int Cost, CardType Type, CardRarity Rarity,
    DuchessEffect[] Effects, int Hits = 1, bool All = false, bool Exhaust = false,
    bool Retain = false, bool UpgradeTokens = false, bool Reaction = false,
    int Moment = 0, int RestageDivisor = 0);

public abstract class DuchessCard : CardModel
{
    protected DuchessCardSpec Spec => DuchessCardCatalog.All[GetType().Name];
    protected DuchessCard(string key) : base(DuchessCardCatalog.All[key].Cost,
        DuchessCardCatalog.All[key].Type, DuchessCardCatalog.All[key].Rarity, TargetFor(key)) { }

    private static TargetType TargetFor(string key)
    {
        DuchessCardSpec spec = DuchessCardCatalog.All[key];
        if (spec.All) return TargetType.AllEnemies;
        return spec.Type == CardType.Attack || spec.Effects.Any(e => e.Kind is "Weak" or "Vulnerable")
            ? TargetType.AnyEnemy : TargetType.Self;
    }

    public override string PortraitPath => this switch
    {
        DuchessElegantBearing => "res://images/packed/card_portraits/duchess/duchess_elegance.png",
        DuchessDodge => "res://images/packed/card_portraits/duchess/duchess_turn_aside.png",
        _ => $"res://images/packed/card_portraits/duchess/{Id.Entry.ToLowerInvariant()}.png",
    };
    public override CardPoolModel Pool => this is DuchessDodge
        ? ModelDb.CardPool<TokenCardPool>() : base.Pool;
    public override CardPoolModel VisualCardPool => this is DuchessDodge
        ? ModelDb.CardPool<ColorlessCardPool>() : base.VisualCardPool;
    public override bool GainsBlock => Spec.Effects.Any(e => e.Kind is "Block" or "AllyBlock");
    public override CardMultiplayerConstraint MultiplayerConstraint => this is DuchessQuietSignal or DuchessSharedStage
        ? CardMultiplayerConstraint.MultiplayerOnly : base.MultiplayerConstraint;
    protected override HashSet<CardTag> CanonicalTags => GetType() == typeof(DuchessStrike)
        ? new() { CardTag.Strike } : GetType() == typeof(DuchessDefend)
            ? new() { CardTag.Defend } : new();

    public override HashSet<CardKeyword> CanonicalKeywords
    {
        get
        {
            var result = new HashSet<CardKeyword>();
            if (Spec.Exhaust) result.Add(CardKeyword.Exhaust);
            if (Spec.Retain) result.Add(CardKeyword.Retain);
            return result;
        }
    }

    protected bool IsMomentActive => Spec.Moment > 0 && DuchessMomentPower.Current(Owner) == Spec.Moment;
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
                        (card, _) => card is DuchessCard duchessCard && duchessCard.IsMomentActive
                            ? decimal.Floor(DuchessMomentPower.DamageDealtThisTurn(card) / duchessCard.Spec.RestageDivisor)
                            : 0m);
                    continue;
                }

                yield return effect.Kind switch
                {
                    "Damage" => new DamageVar(effect.Amount, ValueProp.Move),
                    "Block" or "AllyBlock" => new BlockVar(effect.Amount, ValueProp.Move),
                    "Weak" => new PowerVar<WeakPower>(effect.Kind, effect.Amount),
                    "Vulnerable" => new PowerVar<VulnerablePower>(effect.Kind, effect.Amount),
                    "Strength" => new PowerVar<StrengthPower>(effect.Kind, effect.Amount),
                    "Dexterity" => new PowerVar<DexterityPower>(effect.Kind, effect.Amount),
                    "DodgeAtTurnStart" => new PowerVar<DuchessDodgeAtTurnStartPower>(effect.Kind, effect.Amount),
                    "ReactionBlock" => new PowerVar<DuchessReactionBlockPower>(effect.Kind, effect.Amount),
                    "MomentFiveBlock" => new PowerVar<DuchessMomentFiveBlockPower>(effect.Kind, effect.Amount),
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
            if (Spec.Moment > 0) yield return HoverTipFactory.FromPower<DuchessMomentDescriptionPower>();
            foreach (DuchessEffect effect in Spec.Effects)
            {
                if (effect.Kind is "DodgeToDraw" or "DodgeToHand" or "AllyDodge" or "DodgeAtTurnStart")
                    foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<DuchessDodge>(Spec.UpgradeTokens))
                        yield return tip;
                if (effect.Kind == "Weak") yield return HoverTipFactory.FromPower<WeakPower>();
                if (effect.Kind == "Vulnerable") yield return HoverTipFactory.FromPower<VulnerablePower>();
                if (effect.Kind == "Strength") yield return HoverTipFactory.FromPower<StrengthPower>();
                if (effect.Kind == "Dexterity") yield return HoverTipFactory.FromPower<DexterityPower>();
                if (effect.Kind == "DodgeAtTurnStart") yield return HoverTipFactory.FromPower<DuchessDodgeAtTurnStartPower>();
                if (effect.Kind == "ReactionBlock") yield return HoverTipFactory.FromPower<DuchessReactionBlockPower>();
                if (effect.Kind == "MomentFiveBlock") yield return HoverTipFactory.FromPower<DuchessMomentFiveBlockPower>();
                if (effect.Kind == "MomentFiveDraw") yield return HoverTipFactory.FromPower<DuchessMomentFiveDrawPower>();
                if (effect.Kind == "DodgeMoment") yield return HoverTipFactory.FromPower<DuchessDodgeMomentPower>();
                if (effect.Kind == "ShuffleBlock") yield return HoverTipFactory.FromPower<DuchessShuffleBlockPower>();
            }
        }
    }

    protected override void OnUpgrade()
    {
        foreach (DuchessEffect effect in Spec.Effects)
        {
            string key = effect.Kind switch
            {
                "AllyBlock" => "Block",
                "Damage" when Spec.RestageDivisor > 0 => "CalculationBase",
                _ => effect.Kind,
            };
            DynamicVars[key].UpgradeValueBy(effect.Upgraded - effect.Amount);
        }
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (!Spec.Reaction || card != this || oldPileType != PileType.Draw
            || card.Pile?.Type != PileType.Hand || Owner?.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
            return;
        EnergyCost.AddUntilPlayed(-1, true);
        foreach (DuchessReactionBlockPower power in Owner.Creature.Powers.OfType<DuchessReactionBlockPower>().ToArray())
            await power.OnReactionTriggered();
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
        };

        foreach (DuchessEffect effect in Spec.Effects)
        {
            if (!conditions.TryGetValue(effect.Condition, out bool met) || !met) continue;
            string key = effect.Kind switch
            {
                "AllyBlock" => "Block",
                "Damage" when Spec.RestageDivisor > 0 => "CalculatedDamage",
                _ => effect.Kind,
            };
            decimal amount = effect.Kind == "Damage" && Spec.RestageDivisor > 0
                ? DynamicVars.CalculatedDamage.Calculate(play.Target) : DynamicVars[key].BaseValue;
            Creature[] enemies = Spec.All
                ? CombatState.HittableEnemies.Where(e => e.IsAlive).ToArray()
                : play.Target is { IsAlive: true } target && target.Side != Owner.Creature.Side
                    ? new[] { target } : Array.Empty<Creature>();

            switch (effect.Kind)
            {
                case "Damage":
                    for (int i = 0; i < Spec.Hits; i++)
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
                case "Vulnerable": foreach (Creature enemy in enemies) await Apply<VulnerablePower>(context, enemy, amount); break;
                case "Strength": await Apply<StrengthPower>(context, Owner.Creature, amount); break;
                case "Dexterity": await Apply<DexterityPower>(context, Owner.Creature, amount); break;
                case "DodgeToDraw": await AddDodges(Owner, amount, PileType.Draw); break;
                case "DodgeToHand": await AddDodges(Owner, amount, PileType.Hand); break;
                case "AllyDodge":
                    foreach (var ally in CombatState.Players.Where(p => p.Creature.IsAlive))
                        await AddDodges(ally, amount, PileType.Hand);
                    break;
                case "AdvanceMoment": await DuchessMomentPower.Advance(context, Owner.Creature, (int)amount, this); break;
                case "SetMoment": (await DuchessMomentPower.Ensure(context, Owner.Creature)).SetAfterCurrentCard((int)amount); break;
                case "ShuffleHand": await ShuffleSelected(context, PileType.Hand.GetPile(Owner), (int)amount); break;
                case "ShuffleDiscard": await ShuffleSelected(context, PileType.Discard.GetPile(Owner), (int)amount); break;
                case "ShuffleDiscardAll": await CardPileCmd.Shuffle(context, Owner); break;
                case "RewindTurn": await (await DuchessMomentPower.Ensure(context, Owner.Creature)).RestoreTurnStart(context, this); break;
                case "DodgeAtTurnStart": await Apply<DuchessDodgeAtTurnStartPower>(context, Owner.Creature, amount); break;
                case "ReactionBlock": await Apply<DuchessReactionBlockPower>(context, Owner.Creature, amount); break;
                case "MomentFiveBlock": await Apply<DuchessMomentFiveBlockPower>(context, Owner.Creature, amount); break;
                case "MomentFiveDraw": await Apply<DuchessMomentFiveDrawPower>(context, Owner.Creature, amount); break;
                case "DodgeMoment": await Apply<DuchessDodgeMomentPower>(context, Owner.Creature, amount); break;
                case "ShuffleBlock": await Apply<DuchessShuffleBlockPower>(context, Owner.Creature, amount); break;
                default: throw new InvalidOperationException($"Unknown Duchess effect: {effect.Kind}");
            }
        }
    }

    private async Task AddDodges(MegaCrit.Sts2.Core.Entities.Players.Player player, decimal amount, PileType destination)
    {
        for (int i = 0; i < amount; i++)
        {
            DuchessDodge dodge = CombatState.CreateCard<DuchessDodge>(player);
            if (Spec.UpgradeTokens) CardCmd.Upgrade(dodge);
            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(dodge, destination, player,
                destination == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Top);
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
