#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Compatibility;
using NightMustStay.Core.Models.Cards;
using NightMustStay.Core.Models.Revenant;
using K = NightMustStay.Core.Nodes.Vfx.RevenantAttackVfx.Kind;

namespace NightMustStay.Core.Nodes.Vfx;

/// <summary>Presentation-only routing. No extra damage, targeting RNG, combat delays or saved state.</summary>
public static class RevenantAttackEffects
{
    private sealed record HaloRoute(object Combat, Vector2 End);
    private static readonly ConditionalWeakTable<CardModel, HaloRoute> HaloRoutes = new();
    private static readonly ulong[] LastSound = new ulong[13];
    private static readonly bool[] Played = new bool[13];

    public static K? KindFor(CardModel? card) => card switch
    {
        Halo or ThreefoldHalo or RadagonHalo => K.HaloOut,
        AncientDragonLightning or LansseaxBlade or AncientDragonSpear or FlannSaxLightningSpear => K.LightningRed,
        PreciseLightningStrike or LightningStrike or LightningSpear or DeathLightning => K.LightningYellow,
        IceLightningSpear => K.LightningBlue,
        Beaststone or GurranqsRock => K.BeastRock,
        BeastClaw or GurranqBeastClaw => K.BeastClaw,
        SpaceRendingFrenzy or UnbearableFrenzy or FrenziedFlame => K.Frenzy,
        StrikeRevenant or CursedClawCombo or SoulChargingClaw => K.CursedClaw,
        _ => null
    };

    public static K FamilyKind(RevenantFamilyId? family) => family switch
    {
        RevenantFamilyId.Helen => K.Helen,
        RevenantFamilyId.PumpkinHead => K.Frederick,
        RevenantFamilyId.Skeleton => K.Sebastian,
        _ => K.CursedClaw
    };

    public static AttackCommand WithRevenantFx(this AttackCommand command, CardModel card, Creature? target = null)
    {
        K? kind = KindFor(card);
        if (kind == null) return command;
        // One outbound ring for the entire attack, rather than one ring per AOE target.
        if (kind == K.HaloOut)
            return command.WithHitFx().WithAttackerFx(() => CreateHaloOut(card, target));
        return command.WithHitFx().WithHitVfxNode(hit => Create(hit, kind.Value, card.Owner.Creature));
    }

    public static Node2D? Create(Creature target, K kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target == null || target.IsDead) return null;
        var node = target.GetCreatureNode();
        if (node == null) return null;
        Vector2 end = node.VfxSpawnPosition;
        Vector2 start = attacker?.GetCreatureNode()?.VfxSpawnPosition ?? end - new Vector2(200, 0);
        return NewEffect(kind, start, end);
    }

    private static Node2D NewEffect(K kind, Vector2 start, Vector2 end) => new RevenantAttackVfx
    {
        AttackKind = kind, Source = start - end, GlobalPosition = end, ZIndex = 20,
        OnStart = () => PlaySound(kind)
    };

    public static void Play(Creature target, K kind, Creature? attacker = null)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || target == null || target.IsDead) return;
        var container = target.GetVfxContainer();
        if (container == null) return;
        var effect = Create(target, kind, attacker);
        if (effect != null) container.AddChildSafely(effect);
    }

    private static Vector2 HandPosition(Creature owner)
    {
        var node = owner.GetCreatureNode();
        // Sample the current rig's string-hand point through all nested transforms;
        // never change creature scale, bounds, or combat markers for the effect.
        if (node?.FindChild("Idle", true, false) is Sprite2D { Texture: not null } idle)
            return idle.ToGlobal(idle.Texture.GetSize() * new Vector2(.20f, -.19f));
        return node?.VfxSpawnPosition ?? Vector2.Zero;
    }

    private static Node2D? CreateHaloOut(CardModel card, Creature? target)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || card.Owner?.Creature == null) return null;
        Creature caster = card.Owner.Creature;
        var combat = card.CombatState;
        if (caster.GetCreatureNode() == null || combat == null) return null;
        Vector2 start = HandPosition(caster);
        Creature? farthest = target ?? combat.HittableEnemies.Where(e => e.IsAlive && e.GetCreatureNode() != null)
            .OrderByDescending(e => e.GetCreatureNode()!.VfxSpawnPosition.DistanceSquaredTo(start)).FirstOrDefault();
        var destination = farthest?.GetCreatureNode();
        if (destination == null) return null;
        Vector2 through = destination.VfxSpawnPosition;
        Vector2 direction = (through - start).Normalized();
        Vector2 end = through + direction * 130;
        HaloRoutes.Remove(card);
        HaloRoutes.Add(card, new HaloRoute(combat, end));
        return NewEffect(K.HaloOut, start, end);
    }

    public static void PlayHaloReturn(CardModel card)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || card.Owner?.Creature is not { IsAlive: true } caster) return;
        var container = caster.GetVfxContainer();
        if (container == null || caster.GetCreatureNode() == null) return;
        Vector2 end = HandPosition(caster);
        // Loading a combat does not serialize cosmetic routes: use the enemy side as fallback.
        Vector2 start = end + new Vector2(540, -20);
        if (HaloRoutes.TryGetValue(card, out HaloRoute? route) && ReferenceEquals(route.Combat, card.CombatState))
            start = route.End;
        HaloRoutes.Remove(card);
        container.AddChildSafely(NewEffect(K.HaloReturn, start, end));
    }

    public static Task Damage(PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature dealer, CardModel card)
    {
        // Do not decorate a prayer's sacrifice damage to a friendly family member.
        if (target.Side != dealer.Side && KindFor(card) is { } kind)
        {
            if (kind == K.HaloOut)
            {
                if (!TestMode.IsOn && !NonInteractiveMode.IsActive && target.GetVfxContainer() is { } container)
                {
                    var effect = CreateHaloOut(card, target);
                    if (effect != null) container.AddChildSafely(effect);
                }
            }
            else Play(target, kind, dealer);
        }
        return Sts2BranchCompat.Damage(context, target, amount, props, dealer, card);
    }

    public static Task FamilyDamage(PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature dealer, CardModel? card, RevenantFamilyId? family)
    {
        Play(target, FamilyKind(family), dealer);
        return Sts2BranchCompat.Damage(context, target, amount, props, dealer, card!);
    }

    public static Task FamilyDamage(PlayerChoiceContext context, IEnumerable<Creature> targets, decimal amount,
        ValueProp props, Creature dealer, CardModel? card, RevenantFamilyId? family)
    {
        Creature[] snapshot = targets.ToArray();
        foreach (Creature target in snapshot) Play(target, FamilyKind(family), dealer);
        return Sts2BranchCompat.Damage(context, snapshot, amount, props, dealer, card!);
    }

    public static async Task Heal(Creature target, decimal amount, bool playAnim = true)
    {
        decimal before = target.CurrentHp;
        bool wasDead = target.IsDead;
        bool custom = playAnim && !TestMode.IsOn && !NonInteractiveMode.IsActive
            && CombatManager.Instance.IsInProgress && target.GetCreatureNode() != null && target.GetVfxContainer() != null;
        // Suppress only the native blue Osty/green cross animation for this prayer.
        // The original command still owns HP, history, hooks, sound and its original wait.
        await CreatureCmd.Heal(target, amount, playAnim && !custom);
        if (!custom || !target.IsAlive || target.CurrentHp <= before) return;
        Play(target, K.Heal);
        target.GetVfxContainer()?.AddChildSafely(NHealNumVfx.Create(target, target.CurrentHp - before));
        if (wasDead) target.GetCreatureNode()?.StartReviveAnim();
        // Native Heal omits this sound for Osty (family creatures), whose old VFX
        // supplied audio. Other recipients have already received it from Heal.
        if (target.Monster is Osty && !CombatManager.Instance.IsEnding)
            SfxCmd.Play("event:/sfx/heal", .65f);
    }

    public static string? SoundFor(K kind) => kind switch
    {
        K.HaloOut => "glass_orb_passive.mp3", K.HaloReturn => "glass_orb_evoke.mp3",
        K.LightningRed => "lightning_orb_evoke.mp3", K.LightningYellow => "lightning_orb_passive.mp3",
        K.LightningBlue => "lightning_orb_channel.mp3", K.BeastRock => "blunt_attack.mp3",
        K.BeastClaw => "heavy_attack.mp3", K.Frenzy => "STS_SFX_BurnCard_v1.mp3",
        K.CursedClaw => "slash_attack.mp3", K.Helen => "dagger_throw.mp3",
        K.Frederick => "heavy_attack.mp3", K.Sebastian => "dark_orb_evoke.mp3", _ => null
    };

    private static void PlaySound(K kind)
    {
        if (TestMode.IsOn || NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding) return;
        var audio = NDebugAudioManager.Instance;
        string? name = SoundFor(kind);
        if (audio == null || name == null) return;
        ulong now = Time.GetTicksMsec(), interval = kind is K.LightningRed or K.LightningYellow or K.LightningBlue or K.Frenzy ? 240UL : 85UL;
        int i = (int)kind;
        if (Played[i] && now - LastSound[i] < interval) return;
        Played[i] = true; LastSound[i] = now;
        audio.Play(name, kind is K.LightningRed or K.LightningYellow or K.LightningBlue ? .5f : .65f);
    }
}
