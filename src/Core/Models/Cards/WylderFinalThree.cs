using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
namespace NightMustStay.Core.Models.Cards;
public sealed class WylderEndlessWill : WylderCard
{
    public WylderEndlessWill() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) => await PowerCmd.Apply<WylderEndlessWillPower>(c, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderEndlessWillPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    internal async Task Trigger() { for (int i = 0; i < Amount; i++) await WylderPursuitPower.Enter(new ThrowingPlayerChoiceContext(), Owner, null, WylderEffectSource.PowerEffect); }
}
public sealed class WylderLastResort : WylderCard
{
    public WylderLastResort() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override bool IsPlayable => Owner == null || PileType.Hand.GetPile(Owner).Cards.Any(card => card != this);
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(new MegaCrit.Sts2.Core.HoverTips.HoverTip(new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.title"), new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.description")));
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (!PileType.Hand.GetPile(Owner).Cards.Any(card => card != this)) return;
        var card = (await CardSelectCmd.FromHand(c, Owner, new CardSelectorPrefs(new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_LAST_RESORT_SELECT.title"), 1), null, this)).FirstOrDefault();
        if (card == null) return;
        WylderGrantedDesperation.Grant(card);
        card.EnergyCost.AddThisCombat(1);
        card.InvokeEnergyCostChanged();
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderShriekOfMilos : WylderCard
{
    public WylderShriekOfMilos() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        var power = Owner.Creature.GetPower<WylderShriekOfMilosPower>()
            ?? await PowerCmd.Apply<WylderShriekOfMilosPower>(c, Owner.Creature, 1m, Owner.Creature, this);
        power?.Schedule(CombatState.RoundNumber + 1);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderShriekOfMilosPower : PowerModel
{
    private int _nextRound = -1;
    private bool _ready;
    private CardPlay _attack;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    internal void Schedule(int round) => _nextRound = round;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side && participants.Contains(Owner) && combatState.RoundNumber == _nextRound)
        {
            _nextRound = -1;
            _ready = true;
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (_ready && play.Card.Owner?.Creature == Owner && play.Card.Type == CardType.Attack)
        {
            _ready = false;
            _attack = play;
        }
        return Task.CompletedTask;
    }

    internal decimal DamageMultiplier(Creature dealer, CardModel source, ValueProp props) =>
        dealer == Owner && source?.Type == CardType.Attack && props.IsPoweredAttack()
            && (_attack?.Card == source || (_ready && _attack == null)) ? 2m : 1m;

    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (_attack != play) return;
        _attack = null;
        if (_nextRound < 0) await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return;
        _ready = false;
        _attack = null;
        if (_nextRound < 0) await PowerCmd.Remove(this);
    }
}
internal static class WylderGrantedDesperation
{
    private sealed class Tag { }
    private sealed class Batch
    {
        public readonly Dictionary<MegaCrit.Sts2.Core.Entities.Players.Player, int> Round = new();
        public readonly HashSet<MegaCrit.Sts2.Core.Entities.Players.Player> Resolving = new();
    }
    private static readonly ConditionalWeakTable<CardModel, Tag> Tags = new();
    private static readonly ConditionalWeakTable<object, Batch> Batches = new();
    internal static bool Is(CardModel card) => card is WylderDesperationCard || Tags.TryGetValue(card, out _);
    internal static void Grant(CardModel card) => Tags.GetOrCreateValue(card);
    internal static bool IsResolving(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        object state = player?.Creature?.CombatState;
        // End-turn Desperation uses one shared player-side resolution window.
        // In multiplayer, a teammate's autoplay can Stun an enemy and trigger
        // hand returns for every player. Those returns must wait until every
        // currently resolving Desperation batch has finished, otherwise the
        // newly inserted hand cards can interrupt another player's autoplay.
        return state != null && Batches.TryGetValue(state, out Batch batch)
            && batch.Resolving.Count > 0;
    }
    internal static bool Start(CardModel card) { var state = card.Owner.Creature.CombatState; var b = Batches.GetOrCreateValue(state); if (b.Round.TryGetValue(card.Owner, out int round) && round == state.RoundNumber) return false; b.Round[card.Owner] = state.RoundNumber; return true; }
    internal static async Task Run(CardModel first, PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || first.Owner?.PlayerCombatState == null || !participants.Contains(first.Owner.Creature)) return;
        var hand = PileType.Hand.GetPile(first.Owner);
        if (hand.Cards.FirstOrDefault() != first || hand.Cards.Any(x => !Is(x)) || !Start(first)) return;
        Batch batch = Batches.GetOrCreateValue(first.Owner.Creature.CombatState);
        batch.Resolving.Add(first.Owner);
        try
        {
            var cards = hand.Cards.ToArray();
            var pendingExecutions = cards.OfType<WylderDesperateSurvival>().ToList();
            foreach (var card in cards)
            {
                if (card is WylderDesperateSurvival execution && !pendingExecutions.Contains(execution))
                    continue;
                if (!await TryAutoplay(card))
                    continue;
                if (card is WylderDesperateSurvival playedExecution)
                    pendingExecutions.Remove(playedExecution);

                for (int index = 0; index < pendingExecutions.Count;)
                {
                    if (await TryAutoplay(pendingExecutions[index]))
                        pendingExecutions.RemoveAt(index);
                    else
                        index++;
                }
            }
        }
        finally
        {
            batch.Resolving.Remove(first.Owner);
        }

        async Task<bool> TryAutoplay(CardModel card)
        {
            if (card.Pile != hand) return false;
            var enemies = card.Owner.Creature.CombatState.GetOpponentsOf(card.Owner.Creature).Where(e => e.IsAlive).ToArray();
            if (enemies.Length == 0) return false;
            Creature target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                var legal = card is WylderDesperationCard desperation
                    ? desperation.GrantedLegalTargets().ToArray()
                    : enemies.Where(card.IsValidTarget).ToArray();
                if (legal.Length == 0) return false;
                target = card.Owner.RunState.Rng.CombatTargets.NextItem(legal);
            }
            if (card.TargetType == TargetType.AnyPlayer) target = card.Owner.Creature;
            await CardCmd.AutoPlay(c, card, target);
            return true;
        }
    }
}
