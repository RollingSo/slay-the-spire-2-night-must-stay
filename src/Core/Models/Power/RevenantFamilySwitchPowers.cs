using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using sts2mod.Core.Models.Revenant;

namespace sts2mod.Core.Models.Power;

public sealed class MutualUnderstandingPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterFamilySwitched(PlayerChoiceContext context)
    {
        var family = RevenantSummonManager.For(Owner.Player).CurrentFamilyCreature;
        if (family is { IsAlive: true })
            await PowerCmd.Apply<StrengthPower>(context, family, Amount, Owner, null);
    }
}

public sealed class ChangeHandsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task AfterFamilyEntered(PlayerChoiceContext context, RevenantFamilyId family) =>
        family == RevenantFamilyId.PumpkinHead
            ? RevenantSummonManager.For(Owner.Player).TriggerResonance(context)
            : Task.CompletedTask;
}

public sealed class RelayPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task AfterFamilySwitched(PlayerChoiceContext context) =>
        RevenantSummonManager.For(Owner.Player).TriggerResonance(context);
}

public sealed class PackUpPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterFamilySwitched(PlayerChoiceContext context, RevenantFamilyId previousFamily)
    {
        if (previousFamily != RevenantFamilyId.Helen)
            return;

        CardPile draw = PileType.Draw.GetPile(Owner.Player);
        int maximum = Math.Min((int)Amount, draw.Cards.Count);
        if (maximum <= 0)
            return;

        CardModel[] selected = (await CardSelectCmd.FromCombatPile(
            context,
            draw,
            Owner.Player,
            new CardSelectorPrefs(
                new LocString("cards", "PACK_UP.selectionScreenPrompt"),
                0,
                maximum))).ToArray();
        foreach (CardModel card in selected)
            await CardPileCmd.Add(card, PileType.Discard);
    }
}
