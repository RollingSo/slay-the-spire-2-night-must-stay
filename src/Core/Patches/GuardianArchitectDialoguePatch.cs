using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves;
using sts2mod.Core.Models.Characters;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch(typeof(TheArchitect), "LoadDialogue")]
    public static class GuardianArchitectDialoguePatch
    {
        public static bool Prefix(TheArchitect __instance)
        {
            if (__instance.Owner?.Character is not Guardian
                && __instance.Owner?.Character is not Ironeye)
                return true;

            ModelId characterId = __instance.Owner.Character.Id;
            EnsureCharacterDialogues(__instance.DialogueSet, characterId.Entry);

            int characterWins = SaveManager.Instance.Progress.GetStatsForCharacter(characterId)?.TotalWins ?? 0;
            int wins = SaveManager.Instance.Progress.Wins;
            List<AncientDialogue> dialogues = __instance.DialogueSet
                .GetValidDialogues(characterId, characterWins, wins, allowAnyCharacterDialogues: false)
                .ToList();

            AccessTools.Field(typeof(TheArchitect), "_dialogue")?.SetValue(__instance, __instance.Rng.NextItem(dialogues));
            return false;
        }

        private static void EnsureCharacterDialogues(AncientDialogueSet dialogueSet, string characterKey)
        {
            if (!dialogueSet.CharacterDialogues.ContainsKey(characterKey))
            {
                dialogueSet.CharacterDialogues[characterKey] = new AncientDialogue[]
                {
                    new AncientDialogue(string.Empty, string.Empty)
                    {
                        VisitIndex = 0,
                        EndAttackers = ArchitectAttackers.Both
                    },
                    new AncientDialogue(string.Empty, string.Empty, string.Empty)
                    {
                        VisitIndex = 1,
                        EndAttackers = ArchitectAttackers.Both
                    },
                    new AncientDialogue(string.Empty, string.Empty, string.Empty)
                    {
                        VisitIndex = 2,
                        EndAttackers = ArchitectAttackers.Both
                    },
                    new AncientDialogue(string.Empty, string.Empty)
                    {
                        VisitIndex = 3,
                        EndAttackers = ArchitectAttackers.Both
                    }
                };
            }

            // The Architect may also be initialized before mod localization.
            dialogueSet.PopulateLocKeys("THE_ARCHITECT");
        }
    }
}
