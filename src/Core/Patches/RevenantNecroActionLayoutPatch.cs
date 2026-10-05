using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Patches;

[HarmonyPatch(typeof(NPowerContainer), "UpdatePositions")]
public static class RevenantNecroActionLayoutPatch
{
    [HarmonyPostfix]
    public static void PutActionsBelowPowers(NPowerContainer __instance, List<NPower> ____powerNodes)
    {
        NPower[] actions = ____powerNodes.Where(node => node.Model is RevenantNecroActionPower).ToArray();
        if (actions.Length == 0) return;
        NPower[] powers = ____powerNodes.Except(actions).ToArray();
        float cell = actions[0].Size.X;
        if (cell <= 0f) return;
        int columns = System.Math.Max(1, Mathf.CeilToInt(__instance.Size.X / cell));
        for (int i = 0; i < powers.Length; i++)
            powers[i].Position = new Vector2(i % columns * cell, i / columns * cell);
        float actionY = System.Math.Max(1, (powers.Length + columns - 1) / columns) * cell;
        for (int i = 0; i < actions.Length; i++)
            actions[i].Position = new Vector2(i * cell, actionY);
    }
}
