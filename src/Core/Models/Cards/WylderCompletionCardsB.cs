using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

// P050–P075 non-Power completion. Native portraits are test assets, not an
// assertion that custom artwork has been approved. Pending effects are owned
// by the shared CardPlay event layer, not by these card instances.
public sealed class WylderTakersFlames : WylderCard
{
    private bool _pendingBreakHeal;

    public WylderTakersFlames() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override string PortraitPath => "res://images/packed/card_portraits/wylder/wylder_takers_flames.png";
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        { new DamageVar(16m, ValueProp.Move), new DynamicVar("Break", 30m), new DynamicVar("Heal", 6m) };
    protected override decimal BreakThreshold => DynamicVars["Break"].BaseValue;
    protected override Task OnBreak(PlayerChoiceContext context)
    {
        _pendingBreakHeal = true;
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "AttackTwoHand");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithWylderFx(WylderAttackVfx.Kind.TakersFlames, Owner.Creature).Execute(context);
        // A successful Break remains valid even if its target died. Heal once,
        // after every native AOE target, never once per damaged enemy. If the
        // last enemy died, native combat teardown owns the settlement instead.
        if (_pendingBreakHeal && (CombatManager.Instance == null || !CombatManager.Instance.IsOverOrEnding))
            await ResolvePendingHeal();
    }

    public override async Task AfterCombatEnd(CombatRoom room) => await ResolvePendingHeal();

    private async Task ResolvePendingHeal()
    {
        if (!_pendingBreakHeal || Owner?.Creature == null) return;
        _pendingBreakHeal = false;
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}
