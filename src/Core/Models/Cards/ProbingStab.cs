using NightMustStay.Core.Nodes.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models;
using NightMustStay.Core.Patches;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class ProbingStab : CardModel
    {
        private const string RetainCountKey = "RetainCount";
        private int _pendingRetainCount;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(6m, ValueProp.Move),
            new DynamicVar(RetainCountKey, 2m),
        };

        public ProbingStab()
            : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithGuardianWeaponFx()
                .Execute(choiceContext);

            _pendingRetainCount += DynamicVars[RetainCountKey].IntValue;
        }

        public override async Task BeforeFlushLate(PlayerChoiceContext context, Player player)
        {
            if (player != Owner || _pendingRetainCount <= 0) return;
            int count = _pendingRetainCount;
            _pendingRetainCount = 0;
            if (!MegaCrit.Sts2.Core.Hooks.Hook.ShouldFlush(player.Creature.CombatState, player)) return;
            var selected = await CardSelectCmd.FromHand(context, player,
                new CardSelectorPrefs(new LocString("cards", "PROBING_STAB.selectionScreenPrompt"), 0, count),
                card => !card.ShouldRetainThisTurn, this);
            foreach (CardModel card in selected.ToArray()) card.GiveSingleTurnRetain();
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
            base.DynamicVars[RetainCountKey].UpgradeValueBy(1m);
        }
    }
}
