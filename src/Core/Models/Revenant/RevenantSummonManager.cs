using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using NightMustStay.Core.Models.Power;
using NightMustStay.Core.Models.Relics;

namespace NightMustStay.Core.Models.Revenant;

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
    public int OriginalHp { get; init; }
    public int MaxHp { get; init; }
    public int DamagePerHit { get; init; }
    public int HitCount { get; init; } = 1;
    public bool IsAlive => Creature is { IsAlive: true };

    public async Task PerformAction(PlayerChoiceContext choiceContext)
    {
        Creature[] enemies = Creature.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (!IsAlive || enemies.Length == 0) return;
        Creature target = Creature.PetOwner.RunState.Rng.CombatTargets.NextItem(enemies);
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetAnimationTrigger("Attack");
        for (int hit = 0; hit < HitCount && target.IsAlive; hit++)
            await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(choiceContext, target, DamagePerHit, ValueProp.Unpowered, Creature, null);
    }
}

public sealed class RevenantSummonManager
{
    // Necro stat formula. "Base" is the fixed value and "Ratio" is the
    // percentage of the source monster's unmodified original stat. Keep these
    // meanings stable when balance values are changed later.
    public const int NecroBaseHp = 7;
    public const decimal NecroHpRatio = 0.15m;
    public const decimal NecroDamageRatio = 0.30m;
    public const int NecroMinimumDamage = 3;
    private const float NecroVisualScale = 1f / 3f;
    private const float NecroOffsetRightOfFamily = 220f;
    private const float FamilyGrowthReferenceHp = 150f;
    private const float FamilyGroundY = -20f;

    private static readonly Dictionary<Player, RevenantSummonManager> Managers = new();
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
    private readonly List<NIntent> _necroIntentNodes = new();
    private Sprite2D _familyVisual;
    private Tween _familyIdleTween;
    private Tween _familyActionTween;
    private Creature _familyCreature;
    private RevenantFamilyAction? _scheduledAction;
    private bool _handlingFamilyDeath;
    private readonly HashSet<Creature> _knownFamilyCreatures = new();

    private RevenantSummonManager(Player owner) => Owner = owner;

    public Player Owner { get; }
    public RevenantFamilyId? CurrentFamilyId { get; private set; }

    public bool HasLivingFamily =>
        CurrentFamilyId is not null && _familyCreature is { IsAlive: true };

    public Creature CurrentFamilyCreature =>
        _familyCreature is { IsAlive: true } ? _familyCreature : null;

    public bool IsFamilyCreature(Creature creature) =>
        creature != null && creature == _familyCreature;

    public bool IsKnownFamilyCreature(Creature creature) =>
        creature != null && _knownFamilyCreatures.Contains(creature);

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

    public static bool TryGetFamilyDisplayName(Creature creature, out string displayName)
    {
        foreach (RevenantSummonManager manager in Managers.Values)
        {
            if (!manager.IsFamilyCreature(creature) || manager.CurrentFamilyId is not RevenantFamilyId family)
                continue;

            string localizationKey = family switch
            {
                RevenantFamilyId.Helen => "REVENANT_FAMILY_HELEN_CHOICE.title",
                RevenantFamilyId.PumpkinHead => "REVENANT_FAMILY_PUMPKIN_HEAD_CHOICE.title",
                RevenantFamilyId.Skeleton => "REVENANT_FAMILY_SKELETON_CHOICE.title",
                _ => throw new ArgumentOutOfRangeException(nameof(family), family, null),
            };
            displayName = new LocString("cards", localizationKey).GetFormattedText();
            return true;
        }

        displayName = null;
        return false;
    }

    public static void NotifyCreatureDeath(Creature creature)
    {
        foreach (RevenantSummonManager manager in Managers.Values.ToArray())
        {
            if (manager.IsFamilyCreature(creature))
                _ = manager.HandleFamilyDeath(creature);
            else if (manager.IsNecroCreature(creature))
                manager.HideDeadNecro(creature);
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
        RevenantFamilyId? previousFamily = HasLivingFamily ? CurrentFamilyId : null;
        RevenantFamilyState selectedState = _families[family];
        bool revivingDeadCurrent = CurrentFamilyId == family && _familyCreature is not { IsAlive: true };
        if (!selectedState.IsAlive || revivingDeadCurrent)
        {
            int initialHp = GetInitialFamilyHp(family);
            selectedState.IsAlive = true;
            selectedState.CurrentHp = initialHp;
            selectedState.MaxHp = initialHp;
            selectedState.RetainedBlock = 0;
        }

        if (HasLivingFamily)
        {
            // Calling while a family member is already present always stacks
            // the selected member's initial HP onto the CURRENT maximum HP.
            // Capture A before SwitchFamily restores the selected member's
            // stored state, otherwise switching silently loses the old maximum.
            int currentMaxHp = _familyCreature.MaxHp;
            int currentHp = _familyCreature.CurrentHp;
            int selectedInitialHp = GetInitialFamilyHp(family);
            if (CurrentFamilyId != family)
                await SwitchFamily(context, family);
            if (_familyCreature is { IsAlive: true })
            {
                int stackedMaxHp = currentMaxHp + selectedInitialHp;
                await CreatureCmd.SetMaxHp(
                    _familyCreature,
                    stackedMaxHp);
                await CreatureCmd.SetCurrentHp(
                    _familyCreature,
                    Math.Min(stackedMaxHp, currentHp + selectedInitialHp));
            }
            SnapshotCurrentFamily();
            await ApplyCallBonuses(context);
            await NotifyFamilyEntered(context, previousFamily, family);
            RefreshScheduledFamilyIntent();
            return;
        }

        await SwitchFamily(context, family);
        await ApplyCallBonuses(context);
        await NotifyFamilyEntered(context, previousFamily, family);
        RefreshScheduledFamilyIntent();
    }

    private async Task NotifyFamilyEntered(
        PlayerChoiceContext context,
        RevenantFamilyId? previousFamily,
        RevenantFamilyId currentFamily)
    {
        bool switched = previousFamily.HasValue && previousFamily.Value != currentFamily;
        if (switched)
        {
            foreach (MutualUnderstandingPower power in Owner.Creature.Powers.OfType<MutualUnderstandingPower>().ToArray())
                await power.AfterFamilySwitched(context);
            foreach (RelayPower power in Owner.Creature.Powers.OfType<RelayPower>().ToArray())
                await power.AfterFamilySwitched(context);
            foreach (PackUpPower power in Owner.Creature.Powers.OfType<PackUpPower>().ToArray())
                await power.AfterFamilySwitched(context, previousFamily.Value);
        }

        bool entered = !previousFamily.HasValue || previousFamily.Value != currentFamily;
        if (entered)
        {
            foreach (ChangeHandsPower power in Owner.Creature.Powers.OfType<ChangeHandsPower>().ToArray())
                await power.AfterFamilyEntered(context, currentFamily);
        }
    }

    private async Task ApplyCallBonuses(PlayerChoiceContext context)
    {
        foreach (SpiritLinkPower power in Owner.Creature.Powers.OfType<SpiritLinkPower>().ToArray())
            await power.AfterFamilyCalled();
        foreach (FollowingShadowPower power in Owner.Creature.Powers.OfType<FollowingShadowPower>().ToArray())
            await power.AfterFamilyCalled(context, CurrentFamilyId);
        if (Owner.GetRelic<MiniatureMakeupTools>() is { } miniatureMakeupTools)
            await miniatureMakeupTools.AfterFamilyCalled(context);
        SnapshotCurrentFamily();
    }

    public async Task IncreaseFamilyMaxHp(decimal amount)
    {
        if (_familyCreature is not { IsAlive: true } || amount <= 0m)
            return;
        await CreatureCmd.GainMaxHp(_familyCreature, amount);
        SnapshotCurrentFamily();
    }

    private static int GetInitialFamilyHp(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => 7,
        RevenantFamilyId.PumpkinHead => 8,
        RevenantFamilyId.Skeleton => 9,
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, null),
    };

    public async Task SwitchFamily(PlayerChoiceContext context, RevenantFamilyId family)
    {
        bool revivingSameFamily = CurrentFamilyId == family && _familyCreature is not { IsAlive: true };
        if (CurrentFamilyId == family && !revivingSameFamily)
            return;

        // CallFamily has already restored a dead selected family to its initial
        // HP. Do not overwrite that reset with the dead Osty's stale snapshot.
        if (!revivingSameFamily)
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
            await NightMustStay.Core.Compatibility.Sts2BranchCompat.LoseBlock(pet, pet.Block - state.RetainedBlock);
        else if (pet.Block < state.RetainedBlock)
            await CreatureCmd.GainBlock(pet, state.RetainedBlock - pet.Block, ValueProp.Unpowered, null);

        CurrentFamilyId = family;
        if (_familyCreature != pet)
        {
            if (_familyCreature != null)
                _familyCreature.MaxHpChanged -= OnFamilyMaxHpChanged;
            pet.MaxHpChanged -= OnFamilyMaxHpChanged;
            pet.MaxHpChanged += OnFamilyMaxHpChanged;
        }
        _familyCreature = pet;
        _knownFamilyCreatures.Add(pet);
        RefreshFamilyVisual(family);
        PositionCurrentNecro();
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

    public void RefreshScheduledFamilyIntent()
    {
        if (CurrentFamilyId is RevenantFamilyId family &&
            _familyCreature is { IsAlive: true } &&
            _scheduledAction is RevenantFamilyAction action)
        {
            RefreshFamilyIntents(family, action);
        }
    }

    private async Task PerformFamilyAction(
        PlayerChoiceContext context,
        RevenantFamilyId family,
        bool first)
    {
        Creature pet = _familyCreature;
        VigorPower vigor = pet?.GetPower<VigorPower>();
        decimal vigorToConsume = vigor?.Amount ?? 0m;
        bool attacked = false;
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
                    {
                        attacked = true;
                        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, target, 4m, ValueProp.Move, pet, null);
                    }
                    await CardPileCmd.Draw(context, 1m, Owner);
                }
                else
                {
                    Creature target = RandomEnemy();
                    if (target != null)
                    {
                        attacked = true;
                        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, target, 4m, ValueProp.Move, pet, null);
                    }
                    await PlayerCmd.GainEnergy(1m, Owner);
                }
                break;
            case RevenantFamilyId.PumpkinHead:
                Creature pumpkinTarget = RandomEnemy();
                if (pumpkinTarget == null)
                    break;
                if (first)
                {
                    attacked = true;
                    await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, pumpkinTarget, 5m, ValueProp.Move, pet, null);
                    if (pumpkinTarget.IsAlive)
                        // Family actions are commanded by the Revenant. Attribute
                        // their debuffs to her so player-owned hooks such as
                        // Sleight of Flesh recognize the application.
                        await PowerCmd.Apply<VulnerablePower>(context, pumpkinTarget, 1m, Owner.Creature, null);
                }
                else
                {
                    attacked = true;
                    for (int i = 0; i < 2 && pumpkinTarget.IsAlive; i++)
                        await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, pumpkinTarget, 5m, ValueProp.Move, pet, null);
                }
                break;
            case RevenantFamilyId.Skeleton:
                if (first)
                {
                    attacked = enemies.Length > 0;
                    await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, enemies, 2m, ValueProp.Move, pet, null);
                    await PowerCmd.Apply<WeakPower>(context, enemies, 1m, Owner.Creature, null);
                }
                else
                {
                    attacked = enemies.Length > 0;
                    await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(context, enemies, 6m, ValueProp.Move, pet, null);
                }
                break;
        }
        if (attacked && vigor is not null && vigorToConsume > 0m)
            await PowerCmd.ModifyAmount(context, vigor, -vigorToConsume, pet, null);
        await NotifySummonActed(context);
    }

    public IReadOnlyList<RevenantNecro> GetNecros() => _necros;
    public IReadOnlyList<RevenantNecro> GetLivingNecros() => _necros.Where(necro => necro.IsAlive).ToArray();
    public bool IsNecroCreature(Creature creature) =>
        creature != null && _necros.Any(necro => necro.Creature == creature);

    public static bool IsRegisteredNecroCreature(Creature creature) =>
        creature != null && Managers.Values.Any(manager => manager.IsNecroCreature(creature));

    public void RegisterNecro(RevenantNecro necro)
    {
        _necros.Add(necro);
    }
    public void RemoveNecro(RevenantNecro necro)
    {
        _necros.Remove(necro);
    }
    public async Task TriggerNecroAction(PlayerChoiceContext context, RevenantNecro necro)
    {
        if (!necro.IsAlive)
            return;

        PlayNecroIntents();
        ClearNecroIntents();
        await necro.PerformAction(context);
        await NotifySummonActed(context);
        if (necro.IsAlive)
            RefreshNecroIntent(necro);
    }

    public async Task TriggerAllNecros(PlayerChoiceContext context)
    {
        foreach (RevenantNecro necro in GetLivingNecros())
            await TriggerNecroAction(context, necro);
    }

    private async Task NotifySummonActed(PlayerChoiceContext context)
    {
        foreach (GhostlyTouchPower power in Owner.Creature.Powers.OfType<GhostlyTouchPower>().ToArray())
            await power.AfterSummonActed(context);
    }

    public async Task TriggerResonance(PlayerChoiceContext context)
    {
        await ExecuteScheduledFamilyAction(context);
        await ScheduleFamilyNormalAction(context);
        await TriggerAllNecros(context);
        foreach (BeastClawMarkPower power in Owner.Creature.Powers.OfType<BeastClawMarkPower>().ToArray())
            await power.AfterResonance(context);
        if (Owner.GetRelic<DeepSeaNight>() is { } deepSeaNight)
            await deepSeaNight.AfterResonance();
        if (Owner.GetRelic<OldPocketPortrait>() is { } oldPocketPortrait)
            await oldPocketPortrait.AfterResonance(context);
    }

    public bool CanBecomeNecro(Creature enemy)
    {
        if (enemy == null || !enemy.IsEnemy || !enemy.IsMonster || enemy.Monster == null)
            return false;
        if (_convertedEnemies.Contains(enemy))
            return false;

        // Bosses themselves cannot become Necros, but secondary enemies in a
        // boss encounter are still valid corpses. The Kin Followers, for
        // example, have MinionPower and are therefore not primary enemies;
        // rejecting every secondary enemy (or every creature in a boss room)
        // made Reanimate Dead do nothing after either follower was defeated.
        if (Owner.Creature.CombatState.Encounter.RoomType == RoomType.Boss && enemy.IsPrimaryEnemy)
            return false;

        return enemy.Monster.ShouldShowInCompendium;
    }

    public void TryRegisterNecro(Creature enemy)
    {
        if (!CanBecomeNecro(enemy))
            return;
        _convertedEnemies.Add(enemy);
        // Creatures in combat own mutable monster instances.  Necro corpses are
        // templates for a later summon, so retain the canonical model instead
        // of trying to call ToMutable() on the live combat instance.
        int originalHp = enemy.MonsterMaxHpBeforeModification ?? enemy.MaxHp;
        _deadEnemies.Add((ModelDb.GetById<MonsterModel>(enemy.Monster.Id), originalHp));
    }

    public void MarkForNextCombat(Creature enemy)
    {
        if (enemy?.Monster == null) return;
        int originalHp = enemy.MonsterMaxHpBeforeModification ?? enemy.MaxHp;
        MonsterModel monster = ModelDb.GetById<MonsterModel>(enemy.Monster.Id);
        Owner.Relics.OfType<RevenantSummonRelicModel>().FirstOrDefault()
            ?.MarkNecroForNextCombat(monster, originalHp);
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
        RevenantNecro[] deadNecros = _necros.Where(necro => !necro.IsAlive).ToArray();
        if (deadNecros.Length > 0)
        {
            RevenantNecro dead = Owner.RunState.Rng.CombatTargets.NextItem(deadNecros);
            // Recreate the monster-backed creature instead of only restoring
            // HP. Monster death animations can leave body/spine state latched;
            // a fresh creature guarantees the revived Necro is visibly alive.
            await SummonNecro(context, dead.SourceMonster, dead.OriginalHp);
            return;
        }

        if (_deadEnemies.Count > 0)
        {
            (MonsterModel monster, int originalHp) corpse =
                Owner.RunState.Rng.CombatTargets.NextItem(_deadEnemies);
            _deadEnemies.Remove(corpse);
            await SummonNecro(context, corpse.monster, corpse.originalHp);
            return;
        }

        // Underworld Reflection promises a random Necro without requiring a
        // corpse.  Keep it functional at the start of a combat by selecting a
        // compendium-visible monster from a non-boss encounter as the visual
        // and HP template.  The resulting ally still uses the shared Necro
        // action/powers instead of the source monster's enemy turn logic.
        MonsterModel[] candidates = ModelDb.AllEncounters
            .Where(encounter => encounter.RoomType != RoomType.Boss)
            .SelectMany(encounter => encounter.AllPossibleMonsters)
            .Where(monster => monster.ShouldShowInCompendium && monster.MaxInitialHp > 0)
            .DistinctBy(monster => monster.Id)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException("No eligible monster templates were found for a random Necro.");

        MonsterModel randomMonster = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        await SummonNecro(context, randomMonster, randomMonster.MaxInitialHp);
    }

    public async Task SummonMarkedNecro(PlayerChoiceContext context)
    {
        RevenantSummonRelicModel relic = Owner.Relics
            .OfType<RevenantSummonRelicModel>()
            .FirstOrDefault();
        if (relic == null || !relic.TryGetPendingNecro(out MonsterModel monster, out int originalHp))
            return;

        await SummonNecro(context, monster, originalHp);
        relic.ClearPendingNecro();
    }

    private async Task SummonNecro(PlayerChoiceContext context, MonsterModel sourceMonster, int originalHp)
    {
        await ReplaceCurrentNecro();
        Creature pet = Owner.Creature.CombatState.CreateCreature(sourceMonster.ToMutable(), Owner.Creature.Side, null);
        await PlayerCmd.AddPet(pet, Owner);
        int maxHp = CalculateNecroMaxHp(originalHp);
        await CreatureCmd.SetMaxAndCurrentHp(pet, maxHp);
        await PowerCmd.Apply<DieForYouPower>(context, pet, 1m, Owner.Creature, null);
        await PowerCmd.Apply<NecromancyPower>(context, pet, 1m, Owner.Creature, null);
        (int damagePerHit, int hitCount) = CalculateNecroAttack(pet);
        var necro = new RevenantNecro
        {
            SourceMonster = sourceMonster,
            Creature = pet,
            OriginalHp = originalHp,
            MaxHp = maxHp,
            DamagePerHit = damagePerHit,
            HitCount = hitCount,
        };
        RegisterNecro(necro);
        ConfigureNecroNode(necro);
    }

    private static int CalculateNecroMaxHp(int originalHp) =>
        NecroBaseHp + Math.Max(0, (int)Math.Floor(originalHp * NecroHpRatio));

    private static (int damagePerHit, int hitCount) CalculateNecroAttack(Creature necro)
    {
        AttackIntent attack = necro.Monster?.MoveStateMachine?.States.Values
            .OfType<MoveState>()
            .SelectMany(move => move.Intents)
            .OfType<AttackIntent>()
            .FirstOrDefault();
        decimal originalDamage = attack?.DamageCalc?.Invoke() ?? 0m;
        int damagePerHit = Math.Max(
            NecroMinimumDamage,
            (int)Math.Floor(Math.Max(0m, originalDamage) * NecroDamageRatio));
        int hitCount = attack is MultiAttackIntent multi ? Math.Max(1, multi.Repeats) : 1;
        return (damagePerHit, hitCount);
    }

    private async Task ReplaceCurrentNecro()
    {
        ClearNecroIntents();
        foreach (RevenantNecro existing in _necros.ToArray())
        {
            _necros.Remove(existing);
            Creature creature = existing.Creature;
            if (creature?.CombatState == null)
                continue;

            foreach (DieForYouPower power in creature.Powers.OfType<DieForYouPower>().ToArray())
                await PowerCmd.Remove(power);
            await CreatureCmd.Kill(creature, force: true);

            ICombatState combatState = creature.CombatState;
            if (combatState != null && combatState.ContainsCreature(creature))
            {
                CombatManager.Instance.RemoveCreature(creature);
                combatState.RemoveCreature(creature);
            }
        }
    }

    private void ConfigureNecroNode(RevenantNecro necro)
    {
        NCreature node = FindCreatureNode(necro.Creature);
        if (node == null)
            return;
        node.Visible = true;
        node.Visuals.Visible = true;
        node.SetDefaultScaleTo(NecroVisualScale, 0f);
        // Enemy art is authored facing the player side.  A revived Necro is a
        // player summon, so flip only its body; the HP bar and intent UI must
        // remain readable and unmirrored.
        if (node.Body != null)
            node.Body.Scale = new Vector2(-Mathf.Abs(node.Body.Scale.X), node.Body.Scale.Y);
        node.ToggleIsInteractable(on: true);
        PositionCurrentNecro();
        RefreshNecroIntent(necro);
    }

    private void PositionCurrentNecro()
    {
        RevenantNecro necro = _necros.FirstOrDefault(entry => entry.IsAlive);
        NCreature necroNode = FindCreatureNode(necro?.Creature);
        if (necroNode == null)
            return;

        NCreature familyNode = FindCreatureNode(_familyCreature ?? Owner.Osty);
        NCreature playerNode = FindCreatureNode(Owner.Creature);
        NCreature anchor = familyNode ?? playerNode;
        if (anchor == null)
            return;

        float offset = familyNode != null
            ? NecroOffsetRightOfFamily
            : NecroOffsetRightOfFamily * 1.75f;
        necroNode.Position = anchor.Position + new Vector2(offset, 10f);
    }

    private void HideDeadNecro(Creature creature)
    {
        ClearNecroIntents();
        NCreature node = FindCreatureNode(creature);
        if (node == null)
            return;
        node.Visuals.Visible = false;
        node.ToggleIsInteractable(on: false);
    }

    public async Task NotifyChargeCompleted(CardModel card)
    {
        foreach (ChantingBlessingPower power in Owner.Creature.Powers.OfType<ChantingBlessingPower>().ToArray())
            await power.AfterChargeCompleted();
        if (Owner.GetRelic<BelieversVowCloth>() is { } believersVowCloth)
            believersVowCloth.AfterChargeCompleted(card);
    }

    public async Task NotifyChargedCardPlayed(PlayerChoiceContext context)
    {
        foreach (HeavyEchoPower power in Owner.Creature.Powers.OfType<HeavyEchoPower>().ToArray())
            await power.AfterChargedCardPlayed(context);
    }

    public void CleanupVisuals()
    {
        ClearFamilyIntents();
        ClearNecroIntents();
        StopFamilyTweens();
        _familyVisual?.QueueFree();
        _familyVisual = null;
    }

    public void PrepareForSceneExit()
    {
        ClearFamilyIntents();
        ClearNecroIntents();
        _familyActionTween?.Kill();
        _familyActionTween = null;
        // Deliberately leave the visual and its idle tween attached to the
        // combat scene so it remains present on the victory/result screen.
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
        if (_familyCreature != null)
            _familyCreature.MaxHpChanged -= OnFamilyMaxHpChanged;
        _familyCreature = null;
        CurrentFamilyId = null;
        _handlingFamilyDeath = false;
    }

    private void OnFamilyMaxHpChanged(int _, int __)
    {
        RefreshFamilyVisualScaleAndPosition();
        PositionCurrentNecro();
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
        RefreshFamilyVisualScaleAndPosition();
        _familyVisual.Rotation = 0f;
        _familyVisual.Modulate = Colors.White;
        // Helen's source art faces left.  Player-side summons face the enemies
        // on the right; the other current family assets are already authored
        // in that direction.
        _familyVisual.FlipH = family == RevenantFamilyId.Helen;
        string file = family switch
        {
            RevenantFamilyId.Helen => "helen.png",
            RevenantFamilyId.PumpkinHead => "frederick.png",
            _ => "sebastian.png",
        };
        _familyVisual.Texture = PreloadManager.Cache.GetTexture2D($"res://revenant_assets/families/{file}");
        StartFamilyIdleAnimation(family);
    }

    // Osty's combat bounds are 204 px high. These values are derived from
    // each 512x512 sprite's visible alpha bounds rather than its canvas size:
    // Helen 409 px => 0.8x Osty, Frederick 488 px => 1.0x Osty,
    // Sebastian 487 px => 1.2x Osty.
    private static float GetFamilyBaseVisualScale(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => 0.3990f,
        RevenantFamilyId.PumpkinHead => 0.4180f,
        RevenantFamilyId.Skeleton => 0.5027f,
        _ => 0.3990f,
    };

    private static float GetFamilyBottomOffset(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => 243f,
        RevenantFamilyId.PumpkinHead => 243f,
        RevenantFamilyId.Skeleton => 242f,
        _ => 243f,
    };

    private float GetFamilyGrowthScale()
    {
        float maxHp = Math.Max(0f, _familyCreature?.MaxHp ?? 0f);
        return Mathf.Lerp(1f, 2f, Mathf.Clamp(maxHp / FamilyGrowthReferenceHp, 0f, 1f));
    }

    private Vector2 GetFamilyVisualBasePosition(RevenantFamilyId family)
    {
        float scale = GetFamilyBaseVisualScale(family) * GetFamilyGrowthScale();
        return new Vector2(0f, FamilyGroundY - GetFamilyBottomOffset(family) * scale);
    }

    private void RefreshFamilyVisualScaleAndPosition()
    {
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual) ||
            CurrentFamilyId is not RevenantFamilyId family)
            return;

        _familyVisual.Scale = Vector2.One * GetFamilyBaseVisualScale(family) * GetFamilyGrowthScale();
        _familyVisual.Position = GetFamilyVisualBasePosition(family);
    }

    private void StartFamilyIdleAnimation(RevenantFamilyId family)
    {
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual))
            return;

        _familyIdleTween?.Kill();
        Vector2 basePosition = GetFamilyVisualBasePosition(family);
        _familyVisual.Position = basePosition;
        _familyVisual.Rotation = 0f;
        float lift = family == RevenantFamilyId.Skeleton ? 3f : 2f;
        float tilt = family == RevenantFamilyId.PumpkinHead ? 0.006f : 0.01f;
        _familyIdleTween = _familyVisual.CreateTween().SetLoops();
        _familyIdleTween.TweenProperty(_familyVisual, "position:y", basePosition.Y - lift, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _familyIdleTween.Parallel().TweenProperty(_familyVisual, "rotation", tilt, 1.5f)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _familyIdleTween.TweenProperty(_familyVisual, "position:y", basePosition.Y, 1.5f)
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
        Vector2 basePosition = GetFamilyVisualBasePosition(family);
        _familyVisual.Position = basePosition;
        _familyVisual.Rotation = 0f;

        Vector2 windup = family switch
        {
            RevenantFamilyId.Helen => basePosition + new Vector2(-12f, -4f),
            RevenantFamilyId.PumpkinHead => basePosition + new Vector2(-18f, 4f),
            _ => basePosition + new Vector2(-8f, -6f),
        };
        Vector2 strike = family switch
        {
            RevenantFamilyId.Helen => basePosition + new Vector2(36f, -2f),
            RevenantFamilyId.PumpkinHead => basePosition + new Vector2(28f, 8f),
            _ => basePosition + new Vector2(20f, 2f),
        };
        if (!first)
            strike = new Vector2(strike.X * 0.65f, strike.Y - 4f);

        _familyActionTween = _familyVisual.CreateTween();
        _familyActionTween.TweenProperty(_familyVisual, "position", windup, 0.10f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _familyActionTween.TweenProperty(_familyVisual, "position", strike, 0.13f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        _familyActionTween.Parallel().TweenProperty(_familyVisual, "rotation", 0.035f, 0.13f);
        _familyActionTween.TweenProperty(_familyVisual, "position", basePosition, 0.24f)
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
                new AbstractIntent[] { new SingleAttackIntent(5), new DebuffIntent() },
            (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.Second) =>
                new AbstractIntent[] { new MultiAttackIntent(5, 2) },
            (RevenantFamilyId.Skeleton, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new SingleAttackIntent(2), new DebuffIntent() },
            _ => new AbstractIntent[] { new SingleAttackIntent(6) },
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

    private void RefreshNecroIntent(RevenantNecro necro)
    {
        NCreature necroNode = FindCreatureNode(necro.Creature);
        if (necroNode?.IntentContainer == null || !necro.IsAlive)
            return;

        ClearNecroIntents();
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();
        AbstractIntent attackIntent = necro.HitCount > 1
            ? new MultiAttackIntent(necro.DamagePerHit, necro.HitCount)
            : new SingleAttackIntent(necro.DamagePerHit);
        NIntent intentNode = NIntent.Create((float)GetHashCode() * 0.01f + 0.15f);
        intentNode.Name = "RevenantNecroIntent";
        necroNode.IntentContainer.AddChild(intentNode);
        intentNode.UpdateIntent(attackIntent, enemies, necro.Creature);
        necroNode.IntentContainer.Modulate = Colors.White;
        _necroIntentNodes.Add(intentNode);
    }

    private void PlayNecroIntents()
    {
        foreach (NIntent intent in _necroIntentNodes.ToArray())
        {
            if (GodotObject.IsInstanceValid(intent))
                intent.PlayPerform();
        }
    }

    private void ClearNecroIntents()
    {
        foreach (NIntent intent in _necroIntentNodes.ToArray())
        {
            if (!GodotObject.IsInstanceValid(intent))
                continue;
            intent.GetParent()?.RemoveChild(intent);
            intent.QueueFree();
        }
        _necroIntentNodes.Clear();
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

[HarmonyPatch(typeof(Creature), nameof(Creature.Name), MethodType.Getter)]
public static class RevenantFamilyCreatureNamePatch
{
    [HarmonyPostfix]
    public static void UseSelectedFamilyName(Creature __instance, ref string __result)
    {
        if (RevenantSummonManager.TryGetFamilyDisplayName(__instance, out string displayName))
            __result = displayName;
    }
}
