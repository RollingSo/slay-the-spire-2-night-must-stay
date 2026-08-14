using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace sts2mod.Core.Models.Power
{
    public sealed class NoAttacksNextTurnPower : PowerModel
    {
        private sealed class Data
        {
            public bool IsActive;
        }

        public override PowerType Type => PowerType.Debuff;

        public override PowerStackType StackType => PowerStackType.Counter;

        protected override object InitInternalData()
        {
            return new Data();
        }

        public override bool TryModifyEnergyCostInCombat(CardModel card, decimal unmodifiedCost, out decimal modifiedCost)
        {
            modifiedCost = unmodifiedCost;
            if (!GetInternalData<Data>().IsActive
                || card.Owner.Creature != base.Owner
                || card.Type != CardType.Attack)
            {
                return false;
            }

            // Amount represents the number of affected turns remaining. Reapplying
            // this power extends its duration, but the surcharge is always exactly 1.
            modifiedCost = unmodifiedCost + 1m;
            return true;
        }

        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
        {
            if (side == base.Owner.Side)
            {
                GetInternalData<Data>().IsActive = true;
                Flash();
            }

            return Task.CompletedTask;
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> creatures)
        {
            if (side != base.Owner.Side || !GetInternalData<Data>().IsActive)
                return;

            await PowerCmd.ModifyAmount(choiceContext, this, -1m, Applier, null);
        }
    }
}
