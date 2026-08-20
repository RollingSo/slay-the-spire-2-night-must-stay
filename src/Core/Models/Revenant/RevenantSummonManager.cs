using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using sts2mod.Core.Models.Power;

namespace sts2mod.Core.Models.Revenant;

public enum RevenantFamilyId
{
    Helen,
    PumpkinHead,
    Skeleton,
}

public enum RevenantFamilyAction
{
    First,
    Second,
}

public sealed class RevenantFamilyState
{
    public bool IsAlive { get; set; } = true;
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int RetainedBlock { get; set; }
}

public sealed class RevenantNecro
{
    public required MonsterModel SourceMonster { get; init; }
    public required Creature Creature { get; init; }
    public int MaxHp { get; init; }
    public bool IsAlive => Creature is { IsAlive: true };

    public async Task PerformAction(PlayerChoiceContext choiceContext)
    {
        Creature[] enemies = Creature.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (!IsAlive || enemies.Length == 0) return;
        Creature target = Creature.PetOwner.RunState.Rng.CombatTargets.NextItem(enemies);
        await CreatureCmd.Damage(choiceContext, target, 10m, ValueProp.Unpowered, Creature, null);
    }
}

public sealed class RevenantSummonManager
{
    private static readonly Dictionary<Player, RevenantSummonManager> Managers = new();
    private static readonly Dictionary<Player, (MonsterModel monster, int originalHp)> MarkedForNextCombat = new();
    private readonly Dictionary<RevenantFamilyId, RevenantFamilyState> _families =
        Enum.GetValues<RevenantFamilyId>().ToDictionary(
            id => id,
            id => new RevenantFamilyState
            {
                CurrentHp = GetInitialFamilyHp(id),
                MaxHp = GetInitialFamilyHp(id),
            });
    private readonly List<RevenantNecro> _necros = new();
    private readonly List<(MonsterModel monster, int originalHp)> _deadEnemies = new();
    private readonly HashSet<Creature> _convertedEnemies = new();
    private readonly List<NIntent> _familyIntentNodes = new();
    private Sprite2D _familyVisual;
    private Tween _familyIdleTween;
    private Tween _familyActionTween;
    private Creature _familyCreature;
    private RevenantFamilyAction? _scheduledAction;
    private bool _handlingFamilyDeath;

    private RevenantSummonManager(Player owner) => Owner = owner;

    public Player Owner { get; }
    public RevenantFamilyId? CurrentFamilyId { get; private set; }

    public bool HasLivingFamily =>
        CurrentFamilyId is not null && _familyCreature is { IsAlive: true };

    public Creature CurrentFamilyCreature =>
        _familyCreature is { IsAlive: true } ? _familyCreature : null;

    public bool IsFamilyCreature(Creature creature) =>
        creature != null && creature == _familyCreature;

    public static RevenantSummonManager For(Player player)
    {
        if (!Managers.TryGetValue(player, out RevenantSummonManager manager))
        {
            manager = new RevenantSummonManager(player);
            Managers[player] = manager;
        }
        return manager;
    }

    public static void Clear(Player player) => Managers.Remove(player);

    public static void NotifyCreatureDeath(Creature creature)
    {
        foreach (RevenantSummonManager manager in Managers.Values.ToArray())
        {
            if (manager.IsFamilyCreature(creature))
                _ = manager.HandleFamilyDeath(creature);
        }
    }

    public RevenantFamilyState GetCurrentFamily()
    {
        SnapshotCurrentFamily();
        return CurrentFamilyId is RevenantFamilyId id ? _families[id] : null;
    }

    public IReadOnlyList<RevenantFamilyId> GetCallableFamilies()
    {
        SnapshotCurrentFamily();
        return Enum.GetValues<RevenantFamilyId>();
    }

    public async Task CallFamily(PlayerChoiceContext context, RevenantFamilyId family)
    {
        RevenantFamilyState selectedState = _families[family];
        if (!selectedState.IsAlive)
        {
            int initialHp = GetInitialFamilyHp(family);
            selectedState.IsAlive = true;
            selectedState.CurrentHp = initialHp;
            selectedState.MaxHp = initialHp;
            selectedState.RetainedBlock = 0;
        }

        if (HasLivingFamily)
        {
            int remainingHp = Math.Max(0, _familyCreature.CurrentHp);
            if (CurrentFamilyId != family)
                await SwitchFamily(context, family);
            if (remainingHp > 0 && _familyCreature is { IsAlive: true })
                await CreatureCmd.GainMaxHp(_familyCreature, remainingHp);
            SnapshotCurrentFamily();
            await ApplyCallBonuses(context);
            return;
        }

        await SwitchFamily(context, family);
        await ApplyCallBonuses(context);
    }

    private async Task ApplyCallBonuses(PlayerChoiceContext context)
    {
        foreach (SpiritLinkPower power in Owner.Creature.Powers.OfType<SpiritLinkPower>().ToArray())
            await power.AfterFamilyCalled();
        foreach (FollowingShadowPower power in Owner.Creature.Powers.OfType<FollowingShadowPower>().ToArray())
            await power.AfterFamilyCalled(context, CurrentFamilyId);
        SnapshotCurrentFamily();
    }

    private static int GetInitialFamilyHp(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => 6,
        RevenantFamilyId.PumpkinHead => 8,
        RevenantFamilyId.Skeleton => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, null),
    };

    public async Task SwitchFamily(PlayerChoiceContext context, RevenantFamilyId family)
    {
        if (CurrentFamilyId == family)
            return;

        SnapshotCurrentFamily();
        Creature pet = Owner.Osty;
        if (pet == null || !pet.IsAlive)
        {
            int summonHp = Math.Max(1, _families[family].MaxHp);
            await OstyCmd.Summon(context, Owner, summonHp, context.LastInvolvedModel);
            pet = Owner.Osty;
        }
        if (pet == null)
            return;

        RevenantFamilyState state = _families[family];
        await CreatureCmd.SetMaxAndCurrentHp(pet, state.MaxHp);
        await CreatureCmd.SetCurrentHp(pet, state.CurrentHp);
        if (pet.Block > state.RetainedBlock)
            await CreatureCmd.LoseBlock(pet, pet.Block - state.RetainedBlock);
        else if (pet.Block < state.RetainedBlock)
            await CreatureCmd.GainBlock(pet, state.RetainedBlock - pet.Block, ValueProp.Unpowered, null);

        CurrentFamilyId = family;
        _familyCreature = pet;
        RefreshFamilyVisual(family);
        await ScheduleFamilyNormalAction(context);
    }

    private void SnapshotCurrentFamily()
    {
        if (CurrentFamilyId is not RevenantFamilyId id)
            return;
        Creature pet = _familyCreature ?? Owner.Osty;
        RevenantFamilyState state = _families[id];
        if (pet == null)
        {
            state.IsAlive = false;
            return;
        }
        state.IsAlive = pet.IsAlive;
        state.CurrentHp = pet.CurrentHp;
        state.MaxHp = pet.MaxHp;
        state.RetainedBlock = pet.Block;
    }

    public async Task ScheduleFamilyNormalAction(PlayerChoiceContext context)
    {
        if (CurrentFamilyId is not RevenantFamilyId family || _familyCreature is not { IsAlive: true })
        {
            await ClearFamilyActionPower();
            ClearFamilyIntents();
            return;
        }

        await ClearFamilyActionPower();
        _scheduledAction = Owner.RunState.Rng.Niche.NextBool()
            ? RevenantFamilyAction.First
            : RevenantFamilyAction.Second;
        await ApplyFamilyActionPower(context, family, _scheduledAction.Value);
        RefreshFamilyIntents(family, _scheduledAction.Value);
    }

    public async Task ExecuteScheduledFamilyAction(PlayerChoiceContext context)
    {
        if (CurrentFamilyId is not RevenantFamilyId family ||
            _familyCreature is not { IsAlive: true } ||
            _scheduledAction is not RevenantFamilyAction action)
        {
            _scheduledAction = null;
            await ClearFamilyActionPower();
            ClearFamilyIntents();
            return;
        }

        PlayFamilyIntents();
        _scheduledAction = null;
        await ClearFamilyActionPower();
        ClearFamilyIntents();
        await PerformFamilyAction(context, family, action == RevenantFamilyAction.First);
        SnapshotCurrentFamily();
    }

    private async Task PerformFamilyAction(
        PlayerChoiceContext context,
        RevenantFamilyId family,
        bool first)
    {
        Creature pet = _familyCreature;
        PlayFamilyActionAnimation(family, first);
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();

        Creature RandomEnemy() => enemies.Length == 0
            ? null
            : Owner.RunState.Rng.CombatTargets.NextItem(enemies);

        switch (family)
        {
            case RevenantFamilyId.Helen:
                if (first)
                {
                    Creature target = RandomEnemy();
                    if (target != null)
                        await CreatureCmd.Damage(context, target, 4m, ValueProp.Unpowered, pet, null);
                    await CardPileCmd.Draw(context, 1m, Owner);
                }
                else
                {
                    Creature target = RandomEnemy();
                    if (target != null)
                        await CreatureCmd.Damage(context, target, 4m, ValueProp.Unpowered, pet, null);
                    await PlayerCmd.GainEnergy(1m, Owner);
                }
                break;
            case RevenantFamilyId.PumpkinHead:
                Creature pumpkinTarget = RandomEnemy();
                if (pumpkinTarget == null)
                    break;
                if (first)
                {
                    await CreatureCmd.Damage(context, pumpkinTarget, 8m, ValueProp.Unpowered, pet, null);
                    if (pumpkinTarget.IsAlive)
                        await PowerCmd.Apply<VulnerablePower>(context, pumpkinTarget, 1m, pet, null);
                }
                else
                {
                    for (int i = 0; i < 2 && pumpkinTarget.IsAlive; i++)
                        await CreatureCmd.Damage(context, pumpkinTarget, 8m, ValueProp.Unpowered, pet, null);
                }
                break;
            case RevenantFamilyId.Skeleton:
                if (first)
                {
                    await CreatureCmd.Damage(context, enemies, 3m, ValueProp.Unpowered, pet, null);
                    await PowerCmd.Apply<WeakPower>(context, enemies, 1m, pet, null);
                }
                else
                {
                    await CreatureCmd.Damage(context, enemies, 7m, ValueProp.Unpowered, pet, null);
                }
                break;
        }
    }

    public IReadOnlyList<RevenantNecro> GetNecros() => _necros;
    public IReadOnlyList<RevenantNecro> GetLivingNecros() => _necros.Where(necro => necro.IsAlive).ToArray();
    public void RegisterNecro(RevenantNecro necro)
    {
        _necros.Add(necro);
    }
    public void RemoveNecro(RevenantNecro necro)
    {
        _necros.Remove(necro);
    }
    public Task TriggerNecroAction(PlayerChoiceContext context, RevenantNecro necro) =>
        necro.IsAlive ? necro.PerformAction(context) : Task.CompletedTask;

    public async Task TriggerAllNecros(PlayerChoiceContext context)
    {
        foreach (RevenantNecro necro in GetLivingNecros())
            await TriggerNecroAction(context, necro);
    }

    public async Task TriggerResonance(PlayerChoiceContext context)
    {
        await ExecuteScheduledFamilyAction(context);
        await ScheduleFamilyNormalAction(context);
        await TriggerAllNecros(context);
        foreach (BeastClawMarkPower power in Owner.Creature.Powers.OfType<BeastClawMarkPower>().ToArray())
            await power.AfterResonance(context);
    }

    public bool CanBecomeNecro(Creature enemy)
    {
        if (enemy == null || !enemy.IsMonster || !enemy.IsPrimaryEnemy || enemy.Monster == null)
            return false;
        if (_convertedEnemies.Contains(enemy) || Owner.Creature.CombatState.Encounter.RoomType == RoomType.Boss)
            return false;
        return enemy.Monster.ShouldShowInCompendium;
    }

    public void TryRegisterNecro(Creature enemy)
    {
        if (!CanBecomeNecro(enemy))
            return;
        _convertedEnemies.Add(enemy);
        _deadEnemies.Add((enemy.Monster.ToMutable(), enemy.MaxHp));
    }

    public void MarkForNextCombat(Creature enemy)
    {
        if (enemy?.Monster == null) return;
        MarkedForNextCombat[Owner] = (enemy.Monster.ToMutable(), enemy.MaxHp);
    }

    public async Task ReviveDeadEnemy(PlayerChoiceContext context)
    {
        if (_deadEnemies.Count == 0) return;
        (MonsterModel monster, int originalHp) corpse = _deadEnemies[^1];
        _deadEnemies.RemoveAt(_deadEnemies.Count - 1);
        await SummonNecro(context, corpse.monster, corpse.originalHp);
    }

    public async Task ReviveRandomNecro(PlayerChoiceContext context)
    {
        RevenantNecro dead = _necros.FirstOrDefault(necro => !necro.IsAlive);
        if (dead != null)
        {
            await CreatureCmd.SetMaxAndCurrentHp(dead.Creature, dead.MaxHp);
            return;
        }
        await ReviveDeadEnemy(context);
    }

    public async Task SummonMarkedNecro(PlayerChoiceContext context)
    {
        if (!MarkedForNextCombat.Remove(Owner, out var marked)) return;
        await SummonNecro(context, marked.monster, marked.originalHp);
    }

    private async Task SummonNecro(PlayerChoiceContext context, MonsterModel sourceMonster, int originalHp)
    {
        Creature pet = Owner.Creature.CombatState.CreateCreature(sourceMonster.ToMutable(), Owner.Creature.Side, null);
        await PlayerCmd.AddPet(pet, Owner);
        int maxHp = Math.Max(1, (int)Math.Ceiling(originalHp * 0.30m));
        await CreatureCmd.SetMaxAndCurrentHp(pet, maxHp);
        await PowerCmd.Apply<DieForYouPower>(context, pet, 1m, Owner.Creature, null);
        await PowerCmd.Apply<NecromancyPower>(context, pet, 1m, Owner.Creature, null);
        RegisterNecro(new RevenantNecro { SourceMonster = sourceMonster, Creature = pet, MaxHp = maxHp });
        NCreature node = FindCreatureNode(pet);
        if (node != null)
            node.Scale *= Vector2.One / 3f;
    }

    public async Task NotifyChargeCompleted()
    {
        foreach (ChantingBlessingPower power in Owner.Creature.Powers.OfType<ChantingBlessingPower>().ToArray())
            await power.AfterChargeCompleted();
    }

    public async Task NotifyChargedCardPlayed(PlayerChoiceContext context)
    {
        foreach (HeavyEchoPower power in Owner.Creature.Powers.OfType<HeavyEchoPower>().ToArray())
            await power.AfterChargedCardPlayed(context);
    }

    public void CleanupVisuals()
    {
        ClearFamilyIntents();
        StopFamilyTweens();
        _familyVisual?.QueueFree();
        _familyVisual = null;
    }

    public async Task HandleFamilyDeath(Creature creature)
    {
        if (!IsFamilyCreature(creature) || _handlingFamilyDeath)
            return;

        _handlingFamilyDeath = true;

        if (CurrentFamilyId is RevenantFamilyId id)
        {
            RevenantFamilyState state = _families[id];
            state.IsAlive = false;
            state.CurrentHp = 0;
            state.RetainedBlock = 0;
        }

        _scheduledAction = null;
        ClearFamilyIntents();
        StopFamilyTweens();
        _familyVisual?.QueueFree();
        _familyVisual = null;
        await ClearFamilyActionPower();
        _familyCreature = null;
        CurrentFamilyId = null;
        _handlingFamilyDeath = false;
    }

    private void RefreshFamilyVisual(RevenantFamilyId family)
    {
        NCreature petNode = FindCreatureNode(_familyCreature);
        if (petNode == null)
            return;
        if (petNode.Body != null)
            petNode.Body.Visible = false;
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual))
        {
            _familyVisual = new Sprite2D
            {
                Name = "RevenantFamilyVisual",
                ZIndex = 0,
                Scale = Vector2.One * 0.38f,
            };
            // The family must share the NCreature canvas layer so battlefield
            // backgrounds cannot cover it. Drawing it as the first child keeps
            // the intent and power UI above the artwork at the same canvas Z.
            petNode.AddChild(_familyVisual);
            petNode.MoveChild(_familyVisual, 0);
        }
        _familyVisual.Position = new Vector2(0, -110);
        _familyVisual.Rotation = 0f;
        _familyVisual.Modulate = Colors.White;
        string file = family switch
        {
            RevenantFamilyId.Helen => "helen.png",
            RevenantFamilyId.PumpkinHead => "frederick.png",
            _ => "sebastian.png",
        };
        _familyVisual.Texture = PreloadManager.Cache.GetTexture2D($"res://revenant_assets/families/{file}");
        StartFamilyIdleAnimation(family);
    }

    private void StartFamilyIdleAnimation(RevenantFamilyId family)
    {
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual))
            return;

        _familyIdleTween?.Kill();
        _familyVisual.Position = new Vector2(0, -110);
        _familyVisual.Rotation = 0f;
        float lift = family == RevenantFamilyId.Skeleton ? 3f : 2f;
        float tilt = family == RevenantFamilyId.PumpkinHead ? 0.006f : 0.01f;
        _familyIdleTween = _familyVisual.CreateTween().SetLoops();
        _familyIdleTween.TweenProperty(_familyVisual, "position:y", -110f - lift, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _familyIdleTween.Parallel().TweenProperty(_familyVisual, "rotation", tilt, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _familyIdleTween.TweenProperty(_familyVisual, "position:y", -110f, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _familyIdleTween.Parallel().TweenProperty(_familyVisual, "rotation", -tilt, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
    }

    private void PlayFamilyActionAnimation(RevenantFamilyId family, bool first)
    {
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual))
            return;

        _familyIdleTween?.Kill();
        _familyActionTween?.Kill();
        _familyVisual.Position = new Vector2(0, -110);
        _familyVisual.Rotation = 0f;

        Vector2 windup = family switch
        {
            RevenantFamilyId.Helen => new Vector2(-12f, -114f),
            RevenantFamilyId.PumpkinHead => new Vector2(-18f, -106f),
            _ => new Vector2(-8f, -116f),
        };
        Vector2 strike = family switch
        {
            RevenantFamilyId.Helen => new Vector2(36f, -112f),
            RevenantFamilyId.PumpkinHead => new Vector2(28f, -102f),
            _ => new Vector2(20f, -108f),
        };
        if (!first)
            strike = new Vector2(strike.X * 0.65f, strike.Y - 4f);

        _familyActionTween = _familyVisual.CreateTween();
        _familyActionTween.TweenProperty(_familyVisual, "position", windup, 0.10f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _familyActionTween.TweenProperty(_familyVisual, "position", strike, 0.13f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        _familyActionTween.Parallel().TweenProperty(_familyVisual, "rotation", 0.035f, 0.13f);
        _familyActionTween.TweenProperty(_familyVisual, "position", new Vector2(0, -110), 0.24f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _familyActionTween.Parallel().TweenProperty(_familyVisual, "rotation", 0f, 0.24f);
        _familyActionTween.TweenCallback(Callable.From(() => StartFamilyIdleAnimation(family)));
    }

    private void StopFamilyTweens()
    {
        _familyIdleTween?.Kill();
        _familyIdleTween = null;
        _familyActionTween?.Kill();
        _familyActionTween = null;
    }

    private void RefreshFamilyIntents(RevenantFamilyId family, RevenantFamilyAction action)
    {
        NCreature petNode = FindCreatureNode(_familyCreature);
        if (petNode?.IntentContainer == null)
            return;

        ClearFamilyIntents();
        Creature pet = _familyCreature;
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        IReadOnlyList<AbstractIntent> intents = (family, action) switch
        {
            (RevenantFamilyId.Helen, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new SingleAttackIntent(4) },
            (RevenantFamilyId.Helen, RevenantFamilyAction.Second) =>
                new AbstractIntent[] { new SingleAttackIntent(4), new BuffIntent() },
            (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new SingleAttackIntent(8), new DebuffIntent() },
            (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.Second) =>
                new AbstractIntent[] { new MultiAttackIntent(8, 2) },
            (RevenantFamilyId.Skeleton, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new SingleAttackIntent(3), new DebuffIntent() },
            _ => new AbstractIntent[] { new SingleAttackIntent(7) },
        };

        float startTime = (float)GetHashCode() * 0.01f;
        for (int i = 0; i < intents.Count; i++)
        {
            NIntent intentNode = NIntent.Create(startTime + i * 0.3f);
            intentNode.Name = "RevenantFamilyIntent";
            petNode.IntentContainer.AddChild(intentNode);
            intentNode.UpdateIntent(intents[i], enemies, pet);
            _familyIntentNodes.Add(intentNode);
        }
        petNode.IntentContainer.Modulate = Colors.White;
    }

    private void PlayFamilyIntents()
    {
        foreach (NIntent intent in _familyIntentNodes.ToArray())
        {
            if (GodotObject.IsInstanceValid(intent))
                intent.PlayPerform();
        }
    }

    private void ClearFamilyIntents()
    {
        foreach (NIntent intent in _familyIntentNodes.ToArray())
        {
            if (!GodotObject.IsInstanceValid(intent))
                continue;
            intent.GetParent()?.RemoveChild(intent);
            intent.QueueFree();
        }
        _familyIntentNodes.Clear();
    }

    private async Task ClearFamilyActionPower()
    {
        if (_familyCreature == null)
            return;

        foreach (PowerModel power in _familyCreature.Powers
                     .Where(power => power is IRevenantFamilyActionPower)
                     .ToArray())
        {
            await PowerCmd.Remove(power);
        }
    }

    private async Task ApplyFamilyActionPower(
        PlayerChoiceContext context,
        RevenantFamilyId family,
        RevenantFamilyAction action)
    {
        switch (family, action)
        {
            case (RevenantFamilyId.Helen, RevenantFamilyAction.First):
                await PowerCmd.Apply<HelenStepStrikePower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
            case (RevenantFamilyId.Helen, RevenantFamilyAction.Second):
                await PowerCmd.Apply<HelenRetreatPower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
            case (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.First):
                await PowerCmd.Apply<FrederickHeavyHammerPower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
            case (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.Second):
                await PowerCmd.Apply<FrederickHeadbuttPower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
            case (RevenantFamilyId.Skeleton, RevenantFamilyAction.First):
                await PowerCmd.Apply<SebastianRoarPower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
            default:
                await PowerCmd.Apply<SebastianSlamPower>(context, _familyCreature, 1m, Owner.Creature, null);
                break;
        }
    }

    private static NCreature FindCreatureNode(Creature creature)
    {
        if (creature == null || Engine.GetMainLoop() is not SceneTree tree)
            return null;
        return FindCreatureNode(tree.Root, creature);
    }

    private static NCreature FindCreatureNode(Node node, Creature creature)
    {
        if (node is NCreature candidate && candidate.Entity == creature)
            return candidate;
        foreach (Node child in node.GetChildren())
        {
            NCreature found = FindCreatureNode(child, creature);
            if (found != null) return found;
        }
        return null;
    }
}
