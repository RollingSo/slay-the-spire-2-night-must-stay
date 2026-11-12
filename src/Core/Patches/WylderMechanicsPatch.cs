using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Patches;
[HarmonyPatch]
internal static class WylderPursuitDamageBranchPatch
{
    private static int _dealerIndex;
    private static int _propsIndex;
    private static int _cardSourceIndex;
    private static MethodBase TargetMethod()
    {
        MethodInfo method = typeof(Hook).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == nameof(Hook.ModifyDamage)
                && candidate.GetParameters().Any(parameter => parameter.Name == "dealer")
                && candidate.GetParameters().Any(parameter => parameter.Name == "cardSource"));
        ParameterInfo[] parameters = method.GetParameters();
        _dealerIndex = System.Array.FindIndex(parameters, parameter => parameter.Name == "dealer");
        _propsIndex = System.Array.FindIndex(parameters, parameter => parameter.Name == "props");
        _cardSourceIndex = System.Array.FindIndex(parameters, parameter => parameter.Name == "cardSource");
        return method;
    }

    // Production 0.107 and Public Beta route power modifiers through different
    // PowerModel hook signatures. Hook.ModifyDamage is stable on both branches;
    // inspect its arguments by type so the optional CardPlay slot is irrelevant.
    [HarmonyPostfix]
    private static void Modify(object[] __args, ref decimal __result)
    {
        ModifyDamageHookType hookType = __args.OfType<ModifyDamageHookType>().Single();
        if (!hookType.HasFlag(ModifyDamageHookType.Multiplicative)) return;

        Creature dealer = __args[_dealerIndex] as Creature;
        ValueProp props = __args[_propsIndex] is ValueProp value ? value : default;
        CardModel cardSource = __args[_cardSourceIndex] as CardModel;
        if (dealer == null || cardSource?.Type != CardType.Attack || !props.IsPoweredAttack()) return;

        decimal multiplier = 1m;
        if (dealer.GetPower<WylderPursuitPower>() is { } pursuit)
            multiplier *= pursuit.AttackMultiplier(dealer, props, cardSource);
        __result *= multiplier;
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
