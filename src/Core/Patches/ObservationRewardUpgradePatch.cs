using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace sts2mod.Core.Patches;

internal static class ObservationRewardUpgradeRegistry
{
    private static readonly Dictionary<CardModel, CardModel> Pending = new();

    public static bool IsPending(CardModel card) => Pending.ContainsKey(card);

    public static void Mark(CardModel upgradedCard, CardModel normalCard) =>
        Pending[upgradedCard] = normalCard;

    public static bool TryTake(CardModel upgradedCard, out CardModel normalCard)
    {
        if (!Pending.TryGetValue(upgradedCard, out normalCard))
            return false;
        Pending.Remove(upgradedCard);
        return true;
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.RefreshOptions))]
internal static class ObservationRewardUpgradeScreenPatch
{
    [HarmonyPostfix]
    private static void AfterRefreshOptions(
        NCardRewardSelectionScreen __instance,
        IReadOnlyList<CardCreationResult> options)
    {
        foreach (CardCreationResult option in options)
        {
            CardModel upgradedCard = option.Card;
            if (!ObservationRewardUpgradeRegistry.TryTake(upgradedCard, out CardModel normalCard))
                continue;

            NCardHolder holder = __instance.GetCardHolder(upgradedCard);
            NCard cardNode = holder.CardNode;
            if (cardNode == null)
                continue;

            cardNode.Model = normalCard;
            cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

            Tween reveal = holder.CreateTween();
            // Let the ordinary card finish appearing before the upgrade reveal,
            // so the change is readable instead of happening during the fade-in.
            reveal.TweenInterval(0.65f);
            reveal.TweenCallback(Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(holder) || holder.CardNode == null)
                    return;
                holder.CardNode.Model = upgradedCard;
                holder.CardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                holder.CardNode.Modulate = new Color("D9F05A");
                holder.CardNode.CardHighlight.AnimFlash();
            }));
            reveal.TweenProperty(holder, "scale", holder.SmallScale * 1.08f, 0.12f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            reveal.TweenProperty(holder, "scale", holder.SmallScale, 0.16f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            reveal.Parallel().TweenProperty(cardNode, "modulate", Colors.White, 0.24f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }
}
