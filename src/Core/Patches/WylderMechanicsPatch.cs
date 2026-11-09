using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;
[HarmonyPatch]
internal static class WylderPursuitDamageBranchPatch
{
    private static MethodBase TargetMethod() => DamageModifierBranchCompatibility.Resolve("ModifyDamageMultiplicative");
    [HarmonyPrefix]
    private static bool Modify(AbstractModel __instance, Creature dealer, ValueProp props,
        CardModel cardSource, ref decimal __result)
    {
        if (__instance is WylderShriekOfMilosPower shriek) { __result = shriek.Multiplier(dealer, props, cardSource); return false; }
        if (__instance is WylderForgetTheSwordPower forgotten) { __result = forgotten.Multiplier(dealer, props, cardSource); return false; }
        if (__instance is not WylderPursuitPower power) return true;
        __result = power.AttackMultiplier(dealer, props, cardSource);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TargetType), MethodType.Getter)]
internal static class WylderSweepTargetPatch
{
    [HarmonyPostfix]
    private static void Target(CardModel __instance, ref TargetType __result)
    {
        if (__instance is WylderStampSweep && __instance.IsMutable)
            __result = WylderPursuitPower.IsActive(__instance.Owner?.Creature)
                ? TargetType.AllEnemies : TargetType.AnyEnemy;
    }
}

[HarmonyPatch]
internal static class WylderGrantedDesperationPatch
{
    private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods() =>
     new[] { typeof(AbstractModel) }.Concat(typeof(CardModel).Assembly.GetTypes()).Concat(typeof(WylderCard).Assembly.GetTypes())
      .Where(t => t == typeof(AbstractModel) || typeof(CardModel).IsAssignableFrom(t))
      .Select(t => AccessTools.DeclaredMethod(t, nameof(AbstractModel.BeforeSideTurnEnd))).Where(m => m != null).Distinct();
    [HarmonyPostfix]
    private static void Run(AbstractModel __instance, MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext __0, MegaCrit.Sts2.Core.Combat.CombatSide __1, System.Collections.Generic.IEnumerable<Creature> __2, ref System.Threading.Tasks.Task __result)
    { if (__instance is CardModel card && card is not WylderDesperationCard && WylderGrantedDesperation.Is(card)) __result = Finish(__result, card, __0, __1, __2); }
    private static async System.Threading.Tasks.Task Finish(System.Threading.Tasks.Task original, CardModel card, MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext c, MegaCrit.Sts2.Core.Combat.CombatSide side, System.Collections.Generic.IEnumerable<Creature> participants) { await original; await WylderGrantedDesperation.Run(card, c, side, participants); }
}

[HarmonyPatch]
internal static class WylderGrantedDesperationTextPatch
{
    private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(CardModel)).Single(m => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
    [HarmonyPostfix] private static void Add(CardModel __instance, ref string __result) { if (__instance is not WylderDesperationCard && WylderGrantedDesperation.Is(__instance)) __result = "[gold]" + new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.title").GetFormattedText() + "[/gold]\n" + __result; }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
internal static class WylderGrantedDesperationHoverPatch
{
    [HarmonyPostfix]
    private static void Add(CardModel __instance, ref System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> __result)
    { if (__instance is not WylderDesperationCard && WylderGrantedDesperation.Is(__instance)) __result = __result.Append(new MegaCrit.Sts2.Core.HoverTips.HoverTip(new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.title"), new MegaCrit.Sts2.Core.Localization.LocString("cards", "WYLDER_DESPERATION.description"))); }
}
