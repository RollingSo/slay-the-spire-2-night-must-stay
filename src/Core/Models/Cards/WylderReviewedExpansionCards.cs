using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards;

// Final horizontal-review proposals from the designated planning chat.
// Verified native portraits remain interim assets, never approved custom art.
public sealed class WylderAssaultStake : WylderDesperationCard
{
    public WylderAssaultStake() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(20m, ValueProp.Move) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Concat(new[]
        { HoverTipFactory.Static(StaticHoverTip.Stun) });
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.QueueAttackMotion(Owner.Creature, "Ultimate");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).CompatFromCard(this).TargetingAllOpponents(CombatState)
            .WithWylderFx(WylderAttackVfx.Kind.FireExplosion, Owner.Creature).Execute(context);
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(enemy => enemy.IsAlive).ToArray())
            await CreatureCmd.Stun(enemy);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
