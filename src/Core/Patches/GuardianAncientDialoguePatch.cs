using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using sts2mod.Core.Models.Characters;

namespace sts2mod.Core.Patches
{
    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.DialogueSet), MethodType.Getter)]
    public static class GuardianAncientDialoguePatch
    {
        [HarmonyPostfix]
        public static void AddGuardianDialogue(AncientEventModel __instance, AncientDialogueSet __result)
        {
            IReadOnlyList<AncientDialogue> guardianDialogues = CreateDialogues(__instance);
            IReadOnlyList<AncientDialogue> ironeyeDialogues = CreateDialogues(__instance);
            if (guardianDialogues == null || ironeyeDialogues == null)
                return;

            // PopulateLocKeys mutates each AncientDialogue with its character-
            // specific localization keys. Never share the same instances across
            // two characters or the later entry would overwrite the former.
            AddCharacterDialogue<Guardian>(__result, guardianDialogues);
            AddCharacterDialogue<Ironeye>(__result, ironeyeDialogues);

            // Dialogue sets can be constructed before mod localization tables are
            // merged. Populate on every access so repeating (.r) Guardian lines are
            // discovered once localization is available.
            __result.PopulateLocKeys(__instance.Id.Entry);
        }

        private static void AddCharacterDialogue<TCharacter>(
            AncientDialogueSet dialogueSet,
            IReadOnlyList<AncientDialogue> dialogues)
            where TCharacter : MegaCrit.Sts2.Core.Models.CharacterModel
        {
            string characterKey = ModelDb.Character<TCharacter>().Id.Entry;
            if (!dialogueSet.CharacterDialogues.ContainsKey(characterKey))
                dialogueSet.CharacterDialogues[characterKey] = dialogues;
        }

        private static IReadOnlyList<AncientDialogue> CreateDialogues(AncientEventModel ancient)
        {
            return ancient switch
            {
                Neow => StandardDialogues(2, 2, 3),
                Darv => StandardDialogues(2, 2, 3),
                Nonupeipe => StandardDialogues(2, 2, 3),
                Orobas => StandardDialogues(2, 2, 3),
                Pael => StandardDialogues(2, 2, 3),
                Tanx => StandardDialogues(2, 2, 3),
                Tezcatara => StandardDialogues(2, 2, 3),
                Vakuu => StandardDialogues(2, 2, 3),
                _ => null
            };
        }

        private static IReadOnlyList<AncientDialogue> StandardDialogues(
            int firstLineCount,
            int repeatingLineCount,
            int lateLineCount)
        {
            return new AncientDialogue[]
            {
                EmptyDialogue(firstLineCount, 0),
                EmptyDialogue(repeatingLineCount, 1),
                EmptyDialogue(lateLineCount, 4)
            };
        }

        private static AncientDialogue EmptyDialogue(int lineCount, int visitIndex)
        {
            string[] silentLines = new string[lineCount];
            for (int i = 0; i < silentLines.Length; i++)
                silentLines[i] = string.Empty;

            return new AncientDialogue(silentLines) { VisitIndex = visitIndex };
        }
    }
}
