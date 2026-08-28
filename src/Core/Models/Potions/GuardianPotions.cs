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
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Potions
{
    public sealed class StalwartShieldGrease : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Common;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.Self;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<FortifyPower>(8m) };
        public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<FortifyPower>() };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            await PowerCmd.Apply<FortifyPower>(choiceContext, Owner.Creature, DynamicVars[nameof(FortifyPower)].BaseValue,
                Owner.Creature, null);
        }
    }

    public sealed class KnightlySpirit : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Uncommon;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.Self;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<GuardCounterPower>(20m) };
        public override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<GuardCounterPower>() };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            await PowerCmd.Apply<GuardCounterPower>(choiceContext, Owner.Creature,
                DynamicVars[nameof(GuardCounterPower)].BaseValue, Owner.Creature, null);
        }
    }

    public sealed class FearlessLiquor : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Rare;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.Self;
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
        {
            for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            {
                CardModel shieldPoke = Owner.Creature.CombatState.CreateCard<ShieldPoke>(Owner);
                CardCmd.Upgrade(shieldPoke);
                await CardPileCmd.AddGeneratedCardToCombat(shieldPoke, PileType.Hand, Owner);
            }
        }
    }
}
