using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using sts2mod.Core.Models.CardPools;
using sts2mod.Core.Models.Cards;
using sts2mod.Core.Models.Characters;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCardPools), MethodType.Getter)]
    public static class ModelDbCharacterCardPoolPatch
    {
        public static void Postfix(ref IEnumerable<CardPoolModel> __result)
        {
            List<CardPoolModel> pools = __result.ToList();
            CardPoolModel[] modPools =
            {
                ModelDb.CardPool<GuardianCardPool>(),
                ModelDb.CardPool<IroneyeCardPool>(),
                ModelDb.CardPool<RevenantCardPool>(),
            };

            foreach (CardPoolModel modPool in modPools)
            {
                if (!pools.Any(pool => pool.Id == modPool.Id))
                    pools.Add(modPool);
            }

            __result = pools;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
    public static class ModelDbCharacterPatch
    {
        public static void Postfix(ref IEnumerable<CharacterModel> __result)
        {
            List<CharacterModel> characters = __result.ToList();
            CharacterModel[] modCharacters =
            {
                ModelDb.Character<Guardian>(),
                ModelDb.Character<Ironeye>(),
                ModelDb.Character<Revenant>(),
            };

            foreach (CharacterModel modCharacter in modCharacters)
            {
                if (!characters.Any(character => character.Id == modCharacter.Id))
                    characters.Add(modCharacter);
            }

            __result = characters;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCards), MethodType.Getter)]
    public static class ModelDbModCardPatch
    {
        public static void Postfix(ref IEnumerable<CardModel> __result)
        {
            List<CardModel> cards = __result.ToList();
            CardPoolModel[] modPools =
            {
                ModelDb.CardPool<GuardianCardPool>(),
                ModelDb.CardPool<IroneyeCardPool>(),
                ModelDb.CardPool<RevenantCardPool>(),
            };

            foreach (CardPoolModel modPool in modPools)
            {
                foreach (CardModel modCard in modPool.AllCards)
                {
                    if (!cards.Any(card => card.Id == modCard.Id))
                        cards.Add(modCard);
                }
            }

            CardModel[] tokenCards =
            {
                ModelDb.Card<ShieldPoke>(),
                ModelDb.Card<Approach>(),
                ModelDb.Card<Retreat>(),
                ModelDb.Card<RevenantFamilyHelenChoice>(),
                ModelDb.Card<RevenantFamilyPumpkinHeadChoice>(),
                ModelDb.Card<RevenantFamilySkeletonChoice>(),
            };
            foreach (CardModel tokenCard in tokenCards)
            {
                if (!cards.Any(card => card.Id == tokenCard.Id))
                    cards.Add(tokenCard);
            }

            __result = cards;
        }
    }

    [HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.AllCards), MethodType.Getter)]
    public static class GuardianTokenCardPoolPatch
    {
        public static void Postfix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
        {
            if (__instance is not TokenCardPool)
                return;

            CardModel[] tokenCards =
            {
                ModelDb.Card<ShieldPoke>(),
                ModelDb.Card<Approach>(),
                ModelDb.Card<Retreat>(),
                ModelDb.Card<RevenantFamilyHelenChoice>(),
                ModelDb.Card<RevenantFamilyPumpkinHeadChoice>(),
                ModelDb.Card<RevenantFamilySkeletonChoice>(),
            };
            foreach (CardModel tokenCard in tokenCards)
            {
                if (!__result.Any(card => card.Id == tokenCard.Id))
                    __result = __result.Concat(new[] { tokenCard });
            }
        }
    }
}
