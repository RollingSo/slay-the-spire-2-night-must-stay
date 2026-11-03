using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(VigorPower), nameof(VigorPower.AfterAttack))]
internal static class WylderUndyingVigorPatch
{
    private static readonly FieldInfo InternalData = AccessTools.Field(typeof(PowerModel), "_internalData");
    private static readonly FieldInfo Attack = AccessTools.Field(AccessTools.Inner(typeof(VigorPower), "Data"), "commandToModify");
    private static readonly FieldInfo StartingAmount = AccessTools.Field(Attack.DeclaringType, "amountWhenAttackStarted");

    [HarmonyPrefix]
    private static bool Before(VigorPower __instance, PlayerChoiceContext choiceContext, AttackCommand command, ref Task __result)
    {
        if (!__instance.Owner.HasPower<WylderUndyingPower>()) return true;
        var data = InternalData.GetValue(__instance);
        if (!ReferenceEquals(Attack.GetValue(data), command)) return true;

        // Only the stacks used by this attack are consumed. Clear the native
        // command binding so the surviving power can bind to the next attack.
        int used = (int)StartingAmount.GetValue(data);
        Attack.SetValue(data, null);
        __result = PowerCmd.ModifyAmount(choiceContext, __instance, -(used - used / 2), null, null);
        return false;
    }
}
