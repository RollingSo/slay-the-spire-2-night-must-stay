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
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(new MegaCrit.Sts2.Core.HoverTips.HoverTip(new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.title"), new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.description")));
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
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
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<VulnerablePower>());
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VulnerablePower>("Vulnerable", 1m) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray()) await PowerCmd.Apply<VulnerablePower>(c, enemy, 1, Owner.Creature, this);
        await PowerCmd.Apply<WylderShriekOfMilosPower>(c, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderShriekOfMilosPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    internal decimal Multiplier(Creature dealer, ValueProp props, CardModel card) => dealer == Owner && card?.Type == CardType.Attack && props.IsPoweredAttack() ? 2m : 1m;
    public override async Task AfterCardPlayedLate(PlayerChoiceContext c, CardPlay play) { if (play.Card.Owner.Creature == Owner && play.Card.Type == CardType.Attack) await PowerCmd.Decrement(this); }
}
internal static class WylderGrantedDesperation
{
    private sealed class Tag { }
    private sealed class Batch { public readonly Dictionary<MegaCrit.Sts2.Core.Entities.Players.Player, int> Round = new(); }
    private static readonly ConditionalWeakTable<CardModel, Tag> Tags = new();
    private static readonly ConditionalWeakTable<object, Batch> Batches = new();
    internal static bool Is(CardModel card) => card is WylderDesperationCard || Tags.TryGetValue(card, out _);
    internal static void Grant(CardModel card) => Tags.GetOrCreateValue(card);
    internal static bool Start(CardModel card) { var state = card.Owner.Creature.CombatState; var b = Batches.GetOrCreateValue(state); if (b.Round.TryGetValue(card.Owner, out int round) && round == state.RoundNumber) return false; b.Round[card.Owner] = state.RoundNumber; return true; }
    internal static async Task Run(CardModel first, PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || first.Owner?.PlayerCombatState == null || !participants.Contains(first.Owner.Creature)) return;
        var hand = PileType.Hand.GetPile(first.Owner);
        if (hand.Cards.FirstOrDefault() != first || hand.Cards.Any(x => !Is(x)) || !Start(first)) return;
        foreach (var card in hand.Cards.ToArray())
        {
            if (card.Pile != hand) continue;
            var enemies = card.Owner.Creature.CombatState.GetOpponentsOf(card.Owner.Creature).Where(e => e.IsAlive).ToArray(); if (enemies.Length == 0) break;
            Creature target = null;
            if (card.TargetType == TargetType.AnyEnemy) { var legal = card is WylderDesperationCard d ? d.GrantedLegalTargets().ToArray() : enemies.Where(card.IsValidTarget).ToArray(); if (legal.Length == 0) continue; target = card.Owner.RunState.Rng.CombatTargets.NextItem(legal); }
            if (card.TargetType == TargetType.AnyPlayer) target = card.Owner.Creature;
            await CardCmd.AutoPlay(c, card, target);
        }
    }
}
