using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Cards;

internal static class FrenzyAttackRegression
{
    private static decimal _lost;
    private static int _commands;
    private static CardModel _card = null!;
    private static Creature _target = null!;
    private static decimal _strength, _vigor;
    private static bool _vulnerable;

    public static void Run()
    {
        var h = new HarmonyLib.Harmony("night-must-stay.tests.frenzy-attack");
        void Patch(MethodBase original, string prefix) => h.Patch(original,
            prefix: new HarmonyMethod(typeof(FrenzyAttackRegression), prefix));
        Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)), nameof(InCombat));
        Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.CombatState)), nameof(Combat));
        Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifyDamage)), nameof(Modifiers));
        var helpers = typeof(FrenziedFlame).Assembly.GetType("NightMustStay.Core.Models.Cards.RevenantCardHelpers")!;
        Patch(AccessTools.Method(helpers, "DamageSummons"), nameof(Sacrifice));
        Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)), nameof(Execute));
        try
        {
            var player = TrackingArrowRegression.MakePlayer();
            AccessTools.Field(typeof(Player), "<Character>k__BackingField").SetValue(player,
                ModelDb.Character<NightMustStay.Core.Models.Characters.Revenant>());
            var owner = new Creature(player, 70, 70) { CombatState = new CombatState() };
            AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, owner);
            _target = new Creature(player, 100, 100);
            foreach (bool upgraded in new[] { false, true })
            foreach (decimal lost in new[] { 0m, 4m, 8m })
            foreach (var status in new[] { (0m, 0m, false), (3m, 0m, false), (0m, 5m, false), (3m, 5m, true) })
            foreach (var card in new CardModel[] { ModelDb.Card<FrenziedFlame>().ToMutable(),
                ModelDb.Card<SpaceRendingFrenzy>().ToMutable(), ModelDb.Card<UnbearableFrenzy>().ToMutable() })
            {
                _card = card; card.Owner = player;
                if (upgraded) card.UpgradeInternal();
                _lost = lost; (_strength, _vigor, _vulnerable) = status; _commands = 0;
                var play = TrackingArrowRegression.MakePlay(card, player);
                // The fixture is a live creature, not a combat UI target.
                AccessTools.Field(typeof(CardPlay), "<Target>k__BackingField")?.SetValue(play, _target);
                var task = (Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, new object?[] { null, play })!;
                task.GetAwaiter().GetResult();
                if (_commands != (card is FrenziedFlame && lost == 0 ? 0 : 1))
                    throw new Exception("Frenzy must resolve as one native attack command.");
                if (AccessTools.Field(card.GetType(), "_resolvedLifeLoss").GetValue(card) != null)
                    throw new Exception("Resolved sacrifice leaked into later previews/replays.");
            }
            Console.WriteLine("PASS frenzy native command, snapshot, upgrades, Strength/Vigor/Vulnerable preview matrix (damage execution stubbed).");
        }
        finally { h.UnpatchAll(h.Id); }
    }

    public static bool InCombat(ref bool __result) { __result = true; return false; }
    public static bool Combat(CardModel __instance, ref ICombatState __result)
    { __result = __instance.Owner?.Creature?.CombatState!; return false; }
    public static bool Sacrifice(ref Task<decimal> __result) { __result = Task.FromResult(_lost); return false; }
    public static bool Execute(AttackCommand __instance, ref Task<AttackCommand> __result)
    {
        _commands++;
        if (!__instance.DamageProps.IsPoweredAttack() || __instance.Attacker != _card.Owner.Creature)
            throw new Exception("Sacrifice attack lost its powered attack dealer/properties.");
        if (!ReferenceEquals(AccessTools.Field(typeof(AttackCommand), "_calculatedDamageVar").GetValue(__instance),
                _card.DynamicVars.CalculatedDamage)) throw new Exception("Attack bypassed its dynamic damage variable.");
        decimal multiplier = _card is FrenziedFlame ? 2m : _card is SpaceRendingFrenzy ? (_card.IsUpgraded ? 5m : 4m) : 1m;
        decimal raw = _lost * multiplier;
        if (_card.DynamicVars.CalculatedDamage.Calculate(_target) != raw) throw new Exception("Sacrifice snapshot is incorrect.");
        _card.DynamicVars.CalculatedDamage.UpdateCardPreview(_card, CardPreviewMode.Normal, _target, true);
        decimal expected = decimal.Floor((raw + _strength + _vigor) * (_vulnerable ? 1.5m : 1m));
        if (decimal.Floor(_card.DynamicVars.CalculatedDamage.PreviewValue) != expected)
            throw new Exception("Frenzy preview omitted/doubled attack modifiers.");
        __result = Task.FromResult(__instance); return false;
    }

    // Invoke the installed game's actual power modifiers, allowing release/Beta
    // signatures, while avoiding the standalone fixture's missing run listeners.
    public static bool Modifiers(object[] __args, ref decimal __result)
    {
        decimal amount = (decimal)__args[4]; var props = (ValueProp)__args[5];
        decimal Apply<T>(string method, decimal quantity, decimal current, bool receiver = false) where T : PowerModel
        {
            var power = ModelDb.Power<T>().ToMutable();
            AccessTools.PropertySetter(typeof(PowerModel), nameof(PowerModel.Owner))
                .Invoke(power, new object[] { receiver ? _target : _card.Owner.Creature });
            power.SetAmount((int)quantity, false);
            var m = AccessTools.Method(typeof(T), method);
            var args = m.GetParameters().Select(p => p.Name switch {
                "target" => (object)_target, "amount" => current, "props" => props,
                "dealer" => receiver ? null : _card.Owner.Creature, "cardSource" => _card, _ => null }).ToArray();
            return (decimal)m.Invoke(power, args)!;
        }
        amount += Apply<StrengthPower>("ModifyDamageAdditive", _strength, amount);
        amount += Apply<VigorPower>("ModifyDamageAdditive", _vigor, amount);
        if (_vulnerable) amount *= Apply<VulnerablePower>("ModifyDamageMultiplicative", 1m, amount, true);
        __result = amount; __args[^1] = Array.Empty<AbstractModel>(); return false;
    }
}
