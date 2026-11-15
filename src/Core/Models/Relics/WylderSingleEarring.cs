using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;

namespace NightMustStay.Core.Models.Relics;

public abstract class WylderRelicModel : RelicModel
{
    public override string PackedIconPath => $"res://wylder_assets/relics/{Id.Entry.ToLowerInvariant()}.png";
    protected override string PackedIconOutlinePath => PackedIconPath.Replace(".png", "_outline.png");
    protected override string BigIconPath => PackedIconPath;
}

public sealed class WylderSingleEarring : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WylderPursuitPower>() };
    public override async Task BeforeCombatStart()
    {
        Flash();
        await WylderPursuitPower.Enter(new BlockingPlayerChoiceContext(), Owner.Creature, null, WylderEffectSource.RelicEffect);
    }
}

public sealed class WylderEarring : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WylderPursuitPower>() };
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.PlayerCombatState.TurnNumber > 3) return;
        Flash();
        await WylderPursuitPower.Enter(context, Owner.Creature, null, WylderEffectSource.RelicEffect);
    }
}

public sealed class WylderDarkGrayWhetstone : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<WylderFireCoating>(true);
    public override async Task BeforeCombatStart()
    {
        WylderFireCoating coating = Owner.Creature.CombatState.CreateCard<WylderFireCoating>(Owner);
        CardCmd.Upgrade(coating);
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(coating, PileType.Hand, Owner);
    }
}

public sealed class WylderSearingSwordOil : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<VigorPower>(2m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<VigorPower>() };
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner) return;
        Flash();
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, DynamicVars[nameof(VigorPower)].BaseValue,
            Owner.Creature, null);
    }
}

public sealed class WylderGodfreyPortrait : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(6m, ValueProp.Unpowered) };
    public async Task OnBreak(PlayerChoiceContext context)
    {
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (enemies.Length == 0) return;
        Flash();
        await CreatureCmd.Damage(context, enemies, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner.Creature);
    }
}

public sealed class WylderSilverTeardrop : WylderRelicModel
{
    [SavedProperty] public bool TriggeredThisCombat { get; set; }
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Heal", 4m) };
    public override Task BeforeCombatStart() { TriggeredThisCombat = false; return Task.CompletedTask; }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (TriggeredThisCombat || play.Card.Owner != Owner || !WylderGrantedDesperation.Is(play.Card)) return;
        TriggeredThisCombat = true;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
    public override Task AfterCombatEnd(CombatRoom room) { TriggeredThisCombat = false; return Task.CompletedTask; }
}

public sealed class WylderKingsNight : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<WylderPursuitPower>() };
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.PlayerCombatState.TurnNumber % 3 != 0) return;
        Flash();
        await WylderPursuitPower.Enter(context, Owner.Creature, null, WylderEffectSource.RelicEffect);
    }
}

public sealed class WylderWarriorJarShard : WylderRelicModel
{
    private int _entries;
    [SavedProperty] public int PursuitEntries { get => _entries; set { AssertMutable(); _entries = value; InvokeDisplayAmountChanged(); } }
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => PursuitEntries % 4;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new PowerVar<StrengthPower>(1m) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    { HoverTipFactory.FromPower<WylderPursuitPower>(), HoverTipFactory.FromPower<StrengthPower>() };
    public async Task OnPursuitEntered(PlayerChoiceContext context)
    {
        PursuitEntries++;
        if (PursuitEntries % 4 != 0) return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(context, Owner.Creature, DynamicVars[nameof(StrengthPower)].BaseValue,
            Owner.Creature, null);
    }
}

public sealed class WylderRedStainedBlade : WylderRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new EnergyVar(1) };
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    { HoverTipFactory.FromPower<WylderPursuitPower>(), HoverTipFactory.ForEnergy(this) };
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || !WylderPursuitPower.IsActive(Owner.Creature)) return;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
