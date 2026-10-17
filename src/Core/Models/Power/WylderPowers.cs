using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

namespace NightMustStay.Core.Models.Power;

// Definition-only model for the icon-bearing card glossary; never applied.
public sealed class WylderBreakDescriptionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

public sealed class WylderPursuitPower : PowerModel
{
    private sealed class Data { public CardPlay Play; public bool Reentered; }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public static bool IsActive(Creature owner) => owner?.HasPower<WylderPursuitPower>() == true;
    public static Task Enter(PlayerChoiceContext context, Creature owner, CardModel source)
    {
        // A fresh entry during the consuming card must survive that card.
        if (owner.GetPower<WylderPursuitPower>() is { } existing)
        {
            existing.GetInternalData<Data>().Reentered = true;
            return Task.CompletedTask;
        }
        return PowerCmd.Apply<WylderPursuitPower>(context, owner, 1m, owner, source);
    }
    internal decimal AttackMultiplier(Creature dealer, ValueProp props, CardModel source) =>
        dealer == Owner && source?.Type == CardType.Attack && props.IsPoweredAttack() ? 1.5m : 1m;
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner.Creature == Owner)
        {
            Data data = GetInternalData<Data>();
            data.Play = play;
            data.Reentered = false;
        }
        return Task.CompletedTask;
    }
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner.Creature != Owner || GetInternalData<Data>().Play != play) return;
        GetInternalData<Data>().Play = null;
        bool draw = play.Card.Type == CardType.Skill;
        bool reentered = GetInternalData<Data>().Reentered;
        GetInternalData<Data>().Reentered = false;
        if (!reentered) await PowerCmd.Remove(this);
        if (draw) await CardPileCmd.Draw(context, 1m, Owner.Player);
    }
    // Deliberately no turn-end hook: pursuit survives ending a turn.
}

public sealed class WylderUppercutStrengthDownPower : TemporaryStrengthPower
{
    // Native temporary enemy Strength loss, matching the project's existing
    // FrightenedBird/AirRendingArrow. The enemy's upcoming turn is affected.
    public override AbstractModel OriginModel => ModelDb.Card<WylderStampUpwardCut>();
    protected override bool IsPositive => false;
}
