using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Relics;
using NightMustStay.Core.Models.Revenant;
using ChargeHarmony = NightMustStay.Core.Models.Cards.Harmony;

internal static class BelieversVowClothRegression
{
    private static CardModel _selected = null!;
    private static int _flashes;

    public static void Run()
    {
        var fixture = new HarmonyLib.Harmony("night-must-stay.tests.believers-vow-cloth");
        void Patch(MethodBase target, string prefix) => fixture.Patch(target,
            prefix: new HarmonyMethod(typeof(BelieversVowClothRegression), prefix));
        Patch(AccessTools.Method(typeof(ChargeReturnPower), nameof(ChargeReturnPower.Schedule)), nameof(Schedule));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Attack));
        Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand)), nameof(SelectFromHand));
        Patch(AccessTools.PropertyGetter(typeof(CardModel), "SelectionScreenPrompt"), nameof(Prompt));
        Type helpers = typeof(ChargeHarmony).Assembly.GetType("NightMustStay.Core.Models.Cards.RevenantCardHelpers")!;
        Patch(AccessTools.Method(helpers, "AddFromDiscard"), nameof(Recover));
        // Apply the production patch, so this also verifies its actual Harmony binding.
        Type cleanupPatch = typeof(BelieversVowCloth).Assembly.GetType(
            "NightMustStay.Core.Patches.RevenantChargeCostCleanupPatch")!;
        fixture.CreateClassProcessor(cleanupPatch).Patch();
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                foreach (CardModel card in ChargeCards())
                    CheckChargeLifetime(card, upgraded);
                CheckAssistedCharge(upgraded, preparation: false);
                CheckAssistedCharge(upgraded, preparation: true);
                var harmonyCard = (ChargeHarmony)ModelDb.Card<ChargeHarmony>().ToMutable();
                if (upgraded) harmonyCard.UpgradeInternal();
                Assert(harmonyCard.Keywords.Contains(CardKeyword.Retain),
                    "Harmony must retain before and after upgrading.");
            }
            CheckOwnershipAndRemovedRelic();
            Console.WriteLine("PASS: Believer's Vow Cloth on all eight base/upgraded charge cards, assisted Harmony/Preparation Ritual plays, native cost cleanup, cross-turn persistence, one-use consumption, repeated-completion guards, ownership, removed relic and Harmony Retain (selection, recovery movement, attack execution and charge-return scheduling stubbed).");
        }
        finally { fixture.UnpatchAll(fixture.Id); }
    }

    private static IEnumerable<CardModel> ChargeCards() => new CardModel[]
    {
        ModelDb.Card<AncientDragonLightning>().ToMutable(),
        ModelDb.Card<BeastClaw>().ToMutable(),
        ModelDb.Card<DeathLightning>().ToMutable(),
        ModelDb.Card<LightningSpear>().ToMutable(),
        ModelDb.Card<GurranqBeastClaw>().ToMutable(),
        ModelDb.Card<Ensemble>().ToMutable(),
        ModelDb.Card<SoulChargingClaw>().ToMutable(),
        ModelDb.Card<WatchfulWaiting>().ToMutable(),
    };

    private static void CheckChargeLifetime(CardModel card, bool upgraded)
    {
        Player owner = MakePlayer(out _);
        card.Owner = owner;
        if (upgraded) card.UpgradeInternal();
        Move(card, PileType.Hand);
        int normalCost = card.EnergyCost.GetResolved();
        var charge = (IRevenantChargeCard)card;
        int flashesBefore = _flashes;
        charge.CompleteCharge(null!).GetAwaiter().GetResult();
        Assert(charge.IsChargeComplete && card.EnergyCost.GetResolved() == 0 && _flashes == flashesBefore + 1,
            card.GetType().Name + " external completion must trigger the relic exactly once.");
        charge.CompleteCharge(null!).GetAwaiter().GetResult();
        Assert(_flashes == flashesBefore + 1, "Already charged cards must not trigger completion twice.");
        card.EndOfTurnCleanup();
        Assert(card.EnergyCost.GetResolved() == 0, "The charge discount must survive the end of turn.");
        charge.ChargeComplete = false;
        card.EnergyCost.AfterCardPlayedCleanup();
        Assert(card.EnergyCost.GetResolved() == normalCost, "Assisted charge discount must expire after use.");
        card.EnergyCost.AfterCardPlayedCleanup();
        Assert(card.EnergyCost.GetResolved() == normalCost, "Assisted charge must not queue an extra free play.");

        Move(card, PileType.Play);
        charge.CompleteCharge(null!).GetAwaiter().GetResult();
        Assert(_flashes == flashesBefore + 2, "A later real charge must trigger the relic again.");
        // Native OnPlayWrapper moves to the result pile before its energy cleanup.
        Move(card, PileType.Discard);
        card.EnergyCost.AfterCardPlayedCleanup();
        Assert(card.EnergyCost.GetResolved() == 0,
            "Direct charge cleanup must preserve the newly earned next-play discount.");
        card.EndOfTurnCleanup();
        Move(card, PileType.Hand);
        Assert(card.EnergyCost.GetResolved() == 0, "Direct charge must still be free on the following turn.");
        charge.ChargeComplete = false;
        card.EnergyCost.AfterCardPlayedCleanup();
        Assert(card.EnergyCost.GetResolved() == normalCost, "Using the charged card must consume its free play.");
    }

    private static void CheckAssistedCharge(bool upgraded, bool preparation)
    {
        Player owner = MakePlayer(out _);
        CardModel source = preparation ? ModelDb.Card<PreparationRitual>().ToMutable() : ModelDb.Card<ChargeHarmony>().ToMutable();
        source.Owner = owner;
        if (upgraded) source.UpgradeInternal();
        Move(source, PileType.Play);
        var target = (AncientDragonLightning)ModelDb.Card<AncientDragonLightning>().ToMutable();
        target.Owner = owner;
        if (upgraded) target.UpgradeInternal();
        Move(target, preparation ? PileType.Discard : PileType.Hand);
        _selected = target;
        int flashesBefore = _flashes;
        var play = TrackingArrowRegression.MakePlay(source, owner);
        AccessTools.Field(typeof(CardPlay), "<Target>k__BackingField")!.SetValue(play,
            new Creature(owner, 100, 100));
        ((Task)AccessTools.Method(source.GetType(), "OnPlay").Invoke(source, new object?[] { null, play })!)
            .GetAwaiter().GetResult();
        Assert(target.IsChargeComplete && target.Pile!.Type == PileType.Hand
            && target.EnergyCost.GetResolved() == 0 && _flashes == flashesBefore + 1,
            source.GetType().Name + " actual play must charge the selected card and trigger its owner's relic.");
        source.EnergyCost.AfterCardPlayedCleanup();
        Assert(target.EnergyCost.GetResolved() == 0, "Assisting card cleanup must not consume the target discount.");
        target.EndOfTurnCleanup();
        Assert(target.EnergyCost.GetResolved() == 0, "Assisted charge discount must survive a turn boundary.");
        target.ChargeComplete = false;
        target.EnergyCost.AfterCardPlayedCleanup();
        Assert(target.EnergyCost.GetResolved() == 2, "Assisted charge should grant exactly one free use.");
    }

    private static void CheckOwnershipAndRemovedRelic()
    {
        Player owner = MakePlayer(out BelieversVowCloth relic);
        Player ally = MakePlayer(out _);
        var allyCard = (AncientDragonLightning)ModelDb.Card<AncientDragonLightning>().ToMutable();
        allyCard.Owner = ally;
        Move(allyCard, PileType.Hand);
        int flashesBefore = _flashes;
        relic.AfterChargeCompleted(allyCard);
        Assert(allyCard.EnergyCost.GetResolved() == 2 && _flashes == flashesBefore,
            "The relic must not discount another player's card.");

        var direct = (AncientDragonLightning)ModelDb.Card<AncientDragonLightning>().ToMutable();
        direct.Owner = owner;
        Move(direct, PileType.Play);
        direct.CompleteCharge(null!).GetAwaiter().GetResult();
        ((List<RelicModel>)AccessTools.Field(typeof(Player), "_relics").GetValue(owner)!).Clear();
        direct.EnergyCost.AfterCardPlayedCleanup();
        Assert(direct.EnergyCost.GetResolved() == 2, "Removing the relic must prevent pending discount restoration.");
    }

    private static Player MakePlayer(out BelieversVowCloth relic)
    {
        Player player = TrackingArrowRegression.MakePlayer();
        AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(player,
            ModelDb.Character<NightMustStay.Core.Models.Characters.Revenant>());
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player,
            new Creature(player, 70, 70));
        relic = (BelieversVowCloth)ModelDb.Relic<BelieversVowCloth>().ToMutable();
        relic.Owner = player;
        relic.Flashed += (_, _) => _flashes++;
        AccessTools.Field(typeof(Player), "_relics").SetValue(player, new List<RelicModel> { relic });
        return player;
    }

    private static void Move(CardModel card, PileType destination)
    {
        if (card.Pile is { } previous)
            ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards").GetValue(previous)!).Remove(card);
        ((List<CardModel>)AccessTools.Field(typeof(CardPile), "_cards")
            .GetValue(CardPile.Get(destination, card.Owner))!).Add(card);
    }

    public static bool Schedule(ref Task __result) { __result = Task.CompletedTask; return false; }
    public static bool Prompt(ref LocString __result)
    { __result = new LocString("cards", "HARMONY.selectionScreenPrompt"); return false; }
    public static bool Attack(AttackCommand __instance, ref Task<AttackCommand> __result)
    { __result = Task.FromResult(__instance); return false; }

    public static bool SelectFromHand(Player player, Func<CardModel, bool>? filter,
        ref Task<IEnumerable<CardModel>> __result)
    {
        Assert(_selected.Owner == player && _selected.Pile?.Type == PileType.Hand
            && (filter?.Invoke(_selected) ?? true), "Harmony must select an eligible uncharged hand card.");
        __result = Task.FromResult<IEnumerable<CardModel>>(new[] { _selected });
        return false;
    }

    public static bool Recover(CardModel card, int count, ref Task<IReadOnlyList<CardModel>> __result)
    {
        Assert(_selected.Owner == card.Owner && _selected.Pile?.Type == PileType.Discard && count == 1,
            "Preparation Ritual must recover one of its owner's discarded cards.");
        Move(_selected, PileType.Hand);
        __result = Task.FromResult<IReadOnlyList<CardModel>>(new[] { _selected });
        return false;
    }

    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
