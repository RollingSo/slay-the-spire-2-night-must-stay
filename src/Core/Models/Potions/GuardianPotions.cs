using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Potions
{
    public sealed class StalwartShieldGrease : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Common;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.AnyPlayer;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<FortifyPower>(8m) };
        public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<FortifyPower>() };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            AssertValidForTargetedPotion(target);
            await PowerCmd.Apply<FortifyPower>(choiceContext, target, DynamicVars[nameof(FortifyPower)].BaseValue,
                Owner.Creature, null);
        }
    }

    public sealed class KnightlySpirit : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Uncommon;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.AnyPlayer;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(15m, ValueProp.Unpowered), new PowerVar<GuardCounterPower>(20m) };
        public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<GuardCounterPower>() };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            AssertValidForTargetedPotion(target);
            await CreatureCmd.GainBlock(target, DynamicVars.Block, null);
            await PowerCmd.Apply<GuardCounterPower>(choiceContext, target,
                DynamicVars[nameof(GuardCounterPower)].BaseValue, Owner.Creature, null);
        }
    }

    public sealed class FearlessLiquor : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Rare;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.AnyPlayer;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            AssertValidForTargetedPotion(target);
            for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            {
                CardModel shieldPoke = target.CombatState.CreateCard<ShieldPoke>(target.Player);
                CardCmd.Upgrade(shieldPoke);
                await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Hand, Owner);
            }
        }
    }
}
