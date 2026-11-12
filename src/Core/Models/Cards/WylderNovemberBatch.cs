using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;
using NightMustStay.Core.Compatibility;
namespace NightMustStay.Core.Models.Cards;

public sealed class WylderDesperateFlurry : WylderDesperationCard
{
    public WylderDesperateFlurry() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new RepeatVar(3) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithWylderFx(WylderAttackVfx.Kind.Greatsword, Owner.Creature).Execute(c);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
public sealed class WylderEndTheNight : WylderDesperationCard
{
    public WylderEndTheNight() : base(5, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(60m, ValueProp.Move) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await WylderEndTheNightVfx.PlayPrelude(Owner.Creature);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).WithEndTheNightFx().Execute(c);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(20m);
}
public sealed class WylderFlawlessSword : WylderCard
{
    public WylderFlawlessSword() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(11m, ValueProp.Move), new BlockVar(30m, ValueProp.Move), new DynamicVar("Break", 30m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).Targeting(p.Target).Execute(c); if (BrokeThisPlay) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, p); }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
public abstract class WylderHandPaymentCard : WylderCard
{
    protected WylderHandPaymentCard(int cost) : base(cost, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected abstract bool ExhaustPayment { get; }
    // As with native Burning Pact, an empty hand skips selection, not later effects.
    protected async Task<bool> Pay(PlayerChoiceContext c)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count == 0)
            return false;

        var prefs = new CardSelectorPrefs(ExhaustPayment ? CardSelectorPrefs.ExhaustSelectionPrompt : CardSelectorPrefs.DiscardSelectionPrompt, 1);
        var selected = (await (ExhaustPayment ? CardSelectCmd.FromHand(c, Owner, prefs, null, this) : CardSelectCmd.FromHandForDiscard(c, Owner, prefs, null, this))).FirstOrDefault();
        if (selected == null) return false;
        if (ExhaustPayment) await Sts2BranchCompat.Exhaust(c, selected); else await CardCmd.Discard(c, selected);
        return true;
    }
}
public sealed class WylderLoneCourage : WylderHandPaymentCard
{
    public WylderLoneCourage() : base(0) { }
    protected override bool ExhaustPayment => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<StrengthPower>("Strength", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<StrengthPower>());
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await Pay(c); await PowerCmd.Apply<WylderLoneCouragePower>(c, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}
public sealed class WylderLoneCouragePower : TemporaryStrengthPower { public override AbstractModel OriginModel => ModelDb.Card<WylderLoneCourage>(); }
public sealed class WylderAgility : WylderHandPaymentCard
{
    public WylderAgility() : base(0) { }
    protected override bool ExhaustPayment => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<DexterityPower>("Dexterity", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<DexterityPower>());
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await Pay(c); await PowerCmd.Apply<WylderAgilityPower>(c, Owner.Creature, DynamicVars["Dexterity"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars["Dexterity"].UpgradeValueBy(1m);
}
public sealed class WylderAgilityPower : TemporaryDexterityPower { public override AbstractModel OriginModel => ModelDb.Card<WylderAgility>(); }
public sealed class WylderHunt : WylderHandPaymentCard
{
    public WylderHunt() : base(1) { }
    protected override bool ExhaustPayment => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VulnerablePower>("Vulnerable", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VulnerablePower>());
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await Pay(c); foreach (var e in CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray()) await PowerCmd.Apply<VulnerablePower>(c, e, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars["Vulnerable"].UpgradeValueBy(1m);
}
public sealed class WylderVanish : WylderHandPaymentCard
{
    public WylderVanish() : base(1) { }
    protected override bool ExhaustPayment => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<WeakPower>("Weak", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<WeakPower>());
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { await Pay(c); foreach (var e in CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray()) await PowerCmd.Apply<WeakPower>(c, e, DynamicVars["Weak"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => DynamicVars["Weak"].UpgradeValueBy(1m);
}
public sealed class WylderGamble : WylderCard
{
    public WylderGamble() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(4) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CardPileCmd.Draw(c, DynamicVars.Cards.BaseValue, Owner);
        CardModel[] desperationCards = PileType.Hand.GetPile(Owner).Cards
            .Where(WylderGrantedDesperation.Is).ToArray();
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards
                     .Where(x => !WylderGrantedDesperation.Is(x)).ToArray())
            await CardCmd.Discard(c, card);

        CardModel[] temporaryRetain = desperationCards
            .Where(card => !card.Keywords.Contains(CardKeyword.Retain)).ToArray();
        foreach (CardModel card in temporaryRetain)
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);

        PlayerCmd.EndTurn(Owner, false, () =>
        {
            foreach (CardModel card in temporaryRetain)
                card.RemoveKeyword(CardKeyword.Retain);
            return Task.CompletedTask;
        });
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}
public sealed class WylderPassTheFlame : WylderCard
{
    public WylderPassTheFlame() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { var player = p.Target?.Player; if (player == null || player.Creature.IsDead) return; var candidates = PileType.Draw.GetPile(player).Cards.Where(x => x.Type == CardType.Attack).ToList(); for (int i = 0; i < DynamicVars.Cards.IntValue && candidates.Count > 0; i++) { var card = Owner.RunState.Rng.Niche.NextItem(candidates); candidates.Remove(card); if (card.Pile?.Type != PileType.Draw) continue; await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, this); await CardPileCmd.Draw(c, 1m, player); } }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
public sealed class WylderHeroesAssemble : WylderCard
{
    public WylderHeroesAssemble() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) => await PowerCmd.Apply<WylderHeroesAssemblePower>(c, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
public sealed class WylderHeroesAssemblePower : PowerModel
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    [SavedProperty] public int DueCopies { get; set; }
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state) { if (participants.Contains(Owner)) DueCopies = Amount; return Task.CompletedTask; }
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext c, Player player) { if (player != Owner.Player || DueCopies == 0) return; int copies = Math.Min(Amount, DueCopies); DueCopies = 0; var allies = Owner.CombatState.Players.Where(p => p.Creature.IsAlive).ToArray(); if (copies == Amount) await PowerCmd.Remove(this); else await PowerCmd.ModifyAmount(c, this, -copies, Owner, null); for (int i = 0; i < copies; i++) foreach (var ally in allies) await WylderPursuitPower.Enter(c, ally.Creature, null, WylderEffectSource.PowerEffect); }
}
public sealed class WylderMurderousAura : WylderCard
{
    public WylderMurderousAura() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<StrengthPower>("Strength", 2m), new PowerVar<DexterityPower>("Dexterity", 2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[] { HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>() });
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await PowerCmd.Apply<StrengthPower>(c, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(c, Owner.Creature, DynamicVars["Dexterity"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WylderMurderousAuraPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars["Strength"].UpgradeValueBy(1m); DynamicVars["Dexterity"].UpgradeValueBy(1m); }
}
public sealed class WylderMurderousAuraPower : PowerModel
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override decimal ModifyHandDraw(Player p, decimal count) => p == Owner.Player ? Math.Max(0m, count - Amount) : count;
}
public sealed class WylderForgetTheSword : WylderCard
{
    public WylderForgetTheSword() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<StrengthPower>("Strength", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<StrengthPower>());
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count <= 3)
            await PowerCmd.Apply<StrengthPower>(c, Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["Strength"].UpgradeValueBy(1m);
}
// Combat-local history also covers entries before True Flame is drawn or generated.
internal static class WylderPursuitHistory
{
    private sealed class State { public readonly Dictionary<Player, int> Counts = new(); }
    private static readonly ConditionalWeakTable<object, State> States = new();
    internal static int Count(Player p) => p?.Creature?.CombatState == null ? 0 : States.GetOrCreateValue(p.Creature.CombatState).Counts.GetValueOrDefault(p);
    internal static void Enter(Player p) { if (p?.Creature?.CombatState == null) return; var s = States.GetOrCreateValue(p.Creature.CombatState); s.Counts[p] = s.Counts.GetValueOrDefault(p) + 1; }
}
public sealed class WylderTrueFlame : WylderCard
{
    [SavedProperty] public int SpentEntries { get; set; }
    public WylderTrueFlame() : base(5, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override Task BeforeCombatStart() { SpentEntries = 0; return Task.CompletedTask; }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<VigorPower>("Vigor", 15m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<VigorPower>());
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost) { modifiedCost = originalCost; if (card != this || Owner == null) return false; int discount = Math.Max(0, WylderPursuitHistory.Count(Owner) - SpentEntries); modifiedCost = Math.Max(0m, originalCost - discount); return discount > 0; }
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p) { SpentEntries = WylderPursuitHistory.Count(Owner); await PowerCmd.Apply<VigorPower>(c, Owner.Creature, DynamicVars["Vigor"].BaseValue, Owner.Creature, this); }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
