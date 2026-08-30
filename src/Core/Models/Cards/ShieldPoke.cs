using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Nodes.Vfx;

namespace NightMustStay.Core.Models.Cards
{
    public sealed class ShieldPoke : CardModel
    {
        public override bool GainsBlock => true;

        public override CardPoolModel VisualCardPool => ModelDb.CardPool<ColorlessCardPool>();

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(4m, ValueProp.Move),
            new BlockVar(4m, ValueProp.Move),
            new CardsVar(1),
        };

        public ShieldPoke()
            : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

            decimal fearlessBonus = Fearless.GetShieldPokeDamageBonus(this);
            decimal damage = base.DynamicVars.Damage.BaseValue + fearlessBonus;
            SpearGrindingPower grinding = base.Owner.Creature.GetPower<SpearGrindingPower>();
            for (int i = 0; i < (grinding?.Amount ?? 0); i++)
                damage *= 2m;
            if (fearlessBonus <= 0m)
                await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
            await DamageCmd.Attack(damage)
                .CompatFromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(NightreignHitVfx.CreateGuardianShieldPoke)
                .Execute(choiceContext);
            await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, base.Owner);
        }

        protected override void OnUpgrade()
        {
            base.DynamicVars.Damage.UpgradeValueBy(2m);
            base.DynamicVars.Block.UpgradeValueBy(2m);
        }
    }
}
