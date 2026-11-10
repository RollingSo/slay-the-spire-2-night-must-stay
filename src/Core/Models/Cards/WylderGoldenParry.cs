using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

public sealed class WylderGoldenParry : WylderDesperationCard
{
    public WylderGoldenParry() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<PhantomImbalancePower>("Imbalance", 3m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => base.ExtraHoverTips.Append(HoverTipFactory.FromPower<PhantomImbalancePower>());
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        WylderAnimationPatch.PlayBlock(Owner.Creature);
        foreach (var enemy in CombatState.GetOpponentsOf(Owner.Creature).Where(e => e.IsAlive).ToArray())
        {
            WylderAttackEffects.Play(enemy, WylderAttackVfx.Kind.BucklerImpact, Owner.Creature);
            await PowerCmd.Apply<PhantomImbalancePower>(context, enemy, DynamicVars["Imbalance"].BaseValue, Owner.Creature, this);
            await PhantomImbalancePower.ResolveThreshold(context, enemy);
        }
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
