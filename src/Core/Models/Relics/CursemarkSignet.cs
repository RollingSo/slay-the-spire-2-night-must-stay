using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Relics
{
    public sealed class CursemarkSignet : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        public override string PackedIconPath => "res://ironeye_assets/relics/cursemark_signet.png";

        protected override string PackedIconOutlinePath => PackedIconPath;

        protected override string BigIconPath => PackedIconPath;

        public override bool ShouldFlashOnPlayer => false;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            HoverTipFactory.FromCardWithCardHoverTips<Approach>()
                .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>());

        public override async Task BeforeCombatStart()
        {
            await PowerCmd.Apply<DistancePower>(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                0m,
                Owner.Creature,
                null);
        }

        public override async Task BeforeHandDraw(
            Player player,
            PlayerChoiceContext choiceContext,
            ICombatState combatState)
        {
            if (player != Owner || Owner.PlayerCombatState.TurnNumber > 1)
                return;

            CardModel approach = combatState.CreateCard<Approach>(Owner);
            CardModel retreat = combatState.CreateCard<Retreat>(Owner);
            Flash();
            await CardPileCmd.AddGeneratedCardToCombat(
                approach,
                PileType.Hand,
                Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                retreat,
                PileType.Hand,
                Owner);
        }
    }

    public sealed class RunemarkSignet : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Ancient;

        public override string PackedIconPath => "res://ironeye_assets/relics/runemark_signet.png";

        protected override string PackedIconOutlinePath => PackedIconPath;

        protected override string BigIconPath => PackedIconPath;

        public override bool ShouldFlashOnPlayer => false;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            HoverTipFactory.FromCardWithCardHoverTips<Approach>(true)
                .Concat(HoverTipFactory.FromCardWithCardHoverTips<Retreat>(true));

        public override async Task BeforeCombatStart()
        {
            await PowerCmd.Apply<DistancePower>(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                0m,
                Owner.Creature,
                null);
        }

        public override async Task BeforeHandDraw(
            Player player,
            PlayerChoiceContext choiceContext,
            ICombatState combatState)
        {
            if (player != Owner || Owner.PlayerCombatState.TurnNumber > 1)
                return;

            CardModel approach = combatState.CreateCard<Approach>(Owner);
            CardModel retreat = combatState.CreateCard<Retreat>(Owner);
            CardCmd.Upgrade(approach);
            CardCmd.Upgrade(retreat);

            Flash();
            await CardPileCmd.AddGeneratedCardToCombat(
                approach,
                PileType.Hand,
                Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                retreat,
                PileType.Hand,
                Owner);
        }
    }

}
