using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace NightMustStay.Core.Models.Cards;

public sealed class WylderEochaidsDancingBlade : WylderCard
{
    private AttackCommand _resolvingAttack;
    private int _breakHitCount;
    public WylderEochaidsDancingBlade() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(9m, ValueProp.Move), new DynamicVar("Break", 20m), new RepeatVar(0) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override int BreakHitCount => _breakHitCount;

    // The engine loop asks this after each hit. Scope it to this exact command,
    // so nested damage, other copies and later replays cannot inherit doubling.
    internal bool DoublesHitBudget(AttackCommand command) =>
        ReferenceEquals(command, _resolvingAttack) && BrokeThisPlay;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int hits = ResolveEnergyXValue() + DynamicVars.Repeat.IntValue;
        _breakHitCount = hits;
        _resolvingAttack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this)
            .Targeting(play.Target).WithHitCount(hits);
        try { await _resolvingAttack.Execute(context); }
        finally { _resolvingAttack = null; }
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
