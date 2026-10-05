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

using NightMustStay.Core.Nodes.Vfx;

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
    public bool IsElite { get; init; }
    public RevenantFamilyAction ScheduledAction { get; set; }
    public int DamagePerHit => ScheduledAction == RevenantFamilyAction.First ? 8 : IsElite ? 6 : 3;
    public int HitCount => ScheduledAction == RevenantFamilyAction.First && IsElite ? 2 : 1;
    public bool IsAlive => Creature is { IsAlive: true };

    public async Task PerformAction(PlayerChoiceContext choiceContext)
    {
        Creature[] enemies = Creature.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (!IsAlive || enemies.Length == 0) return;
        Creature target = Creature.PetOwner.RunState.Rng.CombatTargets.NextItem(enemies);
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetAnimationTrigger("Attack");
        for (int hit = 0; hit < HitCount && target.IsAlive; hit++)
        {
            IEnumerable<DamageResult> results = await NightMustStay.Core.Compatibility.Sts2BranchCompat.Damage(
                choiceContext, target, DamagePerHit, ValueProp.Move, Creature, null);
            if (ScheduledAction == RevenantFamilyAction.Second)
                await CreatureCmd.GainBlock(Creature.PetOwner.Creature,
                    results.Sum(result => result.TotalDamage), ValueProp.Unpowered, null);
        }
    }
}

public sealed class RevenantSummonManager
{
    public const int FamilyInitialHp = 6;
    public const int ExistingFamilyCallMaxHpBonus = 6;
    public const int NecroBaseHp = 20;
    public const int EliteNecroHp = 40;
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
    private Node _familyAnimation;
    private Tween _familyIdleTween;
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

    public static void NotifyCreatureNodeReady(NCreature creatureNode)
    {
        Creature creature = creatureNode?.Entity;
        if (creature == null)
            return;

        foreach (RevenantSummonManager manager in Managers.Values)
        {
            if (manager.IsFamilyCreature(creature) &&
                manager.CurrentFamilyId is RevenantFamilyId family)
            {
                // Osty is the backing combat entity for every family member.
                // Summoning and NCreature creation can complete in either
                // order, so retry the visual replacement when the node itself
                // becomes ready instead of leaving Osty's body visible.
                manager.RefreshFamilyVisual(family);
                manager.PositionCurrentNecro();
                manager.RefreshScheduledFamilyIntent();
                return;
            }
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
            if (CurrentFamilyId != family)
                await SwitchFamily(context, family);
            if (_familyCreature is { IsAlive: true })
            {
                (int stackedMaxHp, int stackedCurrentHp) = CalculateFamilyHpIncrease(
                    currentMaxHp,
                    currentHp,
                    ExistingFamilyCallMaxHpBonus);
                await CreatureCmd.SetMaxHp(
                    _familyCreature,
                    stackedMaxHp);
                await CreatureCmd.SetCurrentHp(
                    _familyCreature,
                    stackedCurrentHp);
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

    public async Task IncreaseFamilyMaxAndCurrentHp(int amount)
    {
        if (_familyCreature is not { IsAlive: true } || amount <= 0)
            return;

        (int maxHp, int currentHp) = CalculateFamilyHpIncrease(
            _familyCreature.MaxHp,
            _familyCreature.CurrentHp,
            amount);
        await CreatureCmd.SetMaxHp(_familyCreature, maxHp);
        await CreatureCmd.SetCurrentHp(_familyCreature, currentHp);
        SnapshotCurrentFamily();
    }

    public async Task IncreaseSummonsMaxAndCurrentHp(int amount)
    {
        if (amount <= 0) return;
        await IncreaseFamilyMaxAndCurrentHp(amount);
        foreach (RevenantNecro necro in GetLivingNecros())
        {
            Creature summon = necro.Creature;
            (int maxHp, int currentHp) = CalculateFamilyHpIncrease(summon.MaxHp, summon.CurrentHp, amount);
            await CreatureCmd.SetMaxHp(summon, maxHp);
            await CreatureCmd.SetCurrentHp(summon, currentHp);
        }
    }

    private static int GetInitialFamilyHp(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => FamilyInitialHp,
        RevenantFamilyId.PumpkinHead => FamilyInitialHp,
        RevenantFamilyId.Skeleton => FamilyInitialHp,
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, null),
    };

    private static (int MaxHp, int CurrentHp) CalculateFamilyHpIncrease(
        int currentMaxHp,
        int currentHp,
        int amount)
    {
        int maxHp = currentMaxHp + amount;
        return (maxHp, Math.Min(maxHp, currentHp + amount));
    }

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
                        await RevenantAttackEffects.FamilyDamage(context, target, 3m, ValueProp.Move, pet, null, family);
                    }
                    await CardPileCmd.Draw(context, 1m, Owner);
                }
                else
                {
                    Creature target = RandomEnemy();
                    if (target != null)
                    {
                        attacked = true;
                        await RevenantAttackEffects.FamilyDamage(context, target, 3m, ValueProp.Move, pet, null, family);
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
                    await RevenantAttackEffects.FamilyDamage(context, pumpkinTarget, 5m, ValueProp.Move, pet, null, family);
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
                        await RevenantAttackEffects.FamilyDamage(context, pumpkinTarget, 5m, ValueProp.Move, pet, null, family);
                }
                break;
            case RevenantFamilyId.Skeleton:
                if (first)
                {
                    attacked = enemies.Length > 0;
                    await RevenantAttackEffects.FamilyDamage(context, enemies, 2m, ValueProp.Move, pet, null, family);
                    await PowerCmd.Apply<WeakPower>(context, enemies, 1m, Owner.Creature, null);
                }
                else
                {
                    attacked = enemies.Length > 0;
                    await RevenantAttackEffects.FamilyDamage(context, enemies, 6m, ValueProp.Move, pet, null, family);
                }
                break;
        }
        if (attacked && vigor is not null && vigorToConsume > 0m)
            await PowerCmd.ModifyAmount(context, vigor, -vigorToConsume, pet, null);
        await NotifySummonActed(context);
    }

    public IReadOnlyList<RevenantNecro> GetNecros() => _necros;
    public IReadOnlyList<RevenantNecro> GetLivingNecros() => _necros.Where(necro => necro.IsAlive).ToArray();
    public IReadOnlyList<Creature> GetLivingSummons()
    {
        var summons = new List<Creature>();
        if (_familyCreature is { IsAlive: true }) summons.Add(_familyCreature);
        summons.AddRange(GetLivingNecros().Select(necro => necro.Creature));
        return summons.Distinct().ToArray();
    }
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
        await ClearNecroActionPower(necro);
        await necro.PerformAction(context);
        await NotifySummonActed(context);
        if (necro.IsAlive)
            await ScheduleNecroAction(context, necro);
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

        await SummonRandomNecro(context);
    }

    public async Task SummonRandomNecro(PlayerChoiceContext context)
    {
        // Random summoning never requires a corpse or summons a Family member.
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

    public Task SummonCapturedNecro(PlayerChoiceContext context, MonsterModel sourceMonster) =>
        SummonNecro(context, sourceMonster, sourceMonster.MaxInitialHp);

    private async Task SummonNecro(PlayerChoiceContext context, MonsterModel sourceMonster, int originalHp)
    {
        await ReplaceCurrentNecro();
        Creature pet = Owner.Creature.CombatState.CreateCreature(sourceMonster.ToMutable(), Owner.Creature.Side, null);
        await PlayerCmd.AddPet(pet, Owner);
        bool isElite = IsEliteNecro(sourceMonster);
        int maxHp = CalculateNecroMaxHp(isElite);
        await CreatureCmd.SetMaxAndCurrentHp(pet, maxHp);
        await PowerCmd.Apply<DieForYouPower>(context, pet, 1m, Owner.Creature, null);
        await PowerCmd.Apply<NecromancyPower>(context, pet, 1m, Owner.Creature, null);
        var necro = new RevenantNecro
        {
            SourceMonster = sourceMonster,
            Creature = pet,
            OriginalHp = originalHp,
            MaxHp = maxHp,
            IsElite = isElite,
        };
        RegisterNecro(necro);
        ConfigureNecroNode(necro);
        await ScheduleNecroAction(context, necro);
    }

    internal static int CalculateNecroMaxHp(bool isElite) => isElite ? EliteNecroHp : NecroBaseHp;

    internal static bool IsEliteNecro(MonsterModel monster) => ModelDb.AllEncounters
        .Where(encounter => encounter.RoomType == RoomType.Elite
            && AccessTools.Property(encounter.GetType(), "IsDebugEncounter")?.GetValue(encounter) is not true
            // Newer APIs removed IsDebugEncounter. Native mock encounters must
            // still be excluded, or their full monster lists mark normal foes elite.
            && encounter.GetType().Namespace?.EndsWith(".Mocks", StringComparison.Ordinal) != true
            && encounter.GetType().Name != "DeprecatedEncounter")
        .SelectMany(encounter => encounter.AllPossibleMonsters)
        .Any(candidate => candidate.Id == monster.Id);

    private static async Task ClearNecroActionPower(RevenantNecro necro)
    {
        foreach (PowerModel power in necro.Creature.Powers.OfType<RevenantNecroActionPower>().ToArray())
            await PowerCmd.Remove(power);
    }

    private async Task ScheduleNecroAction(PlayerChoiceContext context, RevenantNecro necro)
    {
        await ClearNecroActionPower(necro);
        necro.ScheduledAction = Owner.RunState.Rng.Niche.NextBool()
            ? RevenantFamilyAction.First : RevenantFamilyAction.Second;
        RevenantNecroActionPower power = necro.ScheduledAction == RevenantFamilyAction.First
            ? (RevenantNecroActionPower)ModelDb.Power<NecroAttackPower>().ToMutable()
            : (RevenantNecroActionPower)ModelDb.Power<NecroProtectPower>().ToMutable();
        power.DynamicVars["Damage"].BaseValue = necro.DamagePerHit;
        power.DynamicVars["Hits"].BaseValue = necro.HitCount;
        await PowerCmd.Apply(context, power, necro.Creature, 1m, Owner.Creature, null);
        RefreshNecroIntent(necro);
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
        _familyAnimation = null;
    }

    public void PrepareForSceneExit()
    {
        ClearFamilyIntents();
        ClearNecroIntents();
        if (_familyAnimation != null && GodotObject.IsInstanceValid(_familyAnimation))
            _familyAnimation.Call("play_trigger", "Idle");
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
        _familyAnimation = null;
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
        if (_familyVisual != null && GodotObject.IsInstanceValid(_familyVisual) &&
            _familyVisual.GetParent() != petNode)
        {
            StopFamilyTweens();
            _familyVisual.QueueFree();
            _familyVisual = null;
            _familyAnimation = null;
        }
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
            _familyAnimation = new Node { Name = "FamilyFrameAnimation" };
            _familyAnimation.SetScript(GD.Load<GDScript>("res://revenant_assets/families/family_animation.gd"));
            _familyVisual.AddChild(_familyAnimation);
            _familyAnimation.Connect("finished", Callable.From(() =>
            {
                if (CurrentFamilyId is RevenantFamilyId current)
                    StartFamilyIdleAnimation(current);
            }));
        }
        RefreshFamilyVisualScaleAndPosition();
        _familyVisual.Rotation = 0f;
        _familyVisual.Modulate = Colors.White;
        // Approved family sprites face the enemies on the right.
        _familyVisual.FlipH = false;
        string file = family switch
        {
            RevenantFamilyId.Helen => "helen.png",
            RevenantFamilyId.PumpkinHead => "frederick.png",
            _ => "sebastian.png",
        };
        _familyVisual.Texture = PreloadManager.Cache.GetTexture2D($"res://revenant_assets/families/{file}");
        _familyAnimation.Call("configure", _familyVisual,
            $"res://revenant_assets/families/animations/{System.IO.Path.GetFileNameWithoutExtension(file)}");
        StartFamilyIdleAnimation(family);
    }

    private static string GetFamilyAssetName(RevenantFamilyId family) => family switch
    {
        RevenantFamilyId.Helen => "helen",
        RevenantFamilyId.PumpkinHead => "frederick",
        _ => "sebastian",
    };

    private static Rect2I GetFamilyIdleBounds(RevenantFamilyId family)
    {
        using Image image = PreloadManager.Cache.GetTexture2D(
            $"res://revenant_assets/families/{GetFamilyAssetName(family)}.png").GetImage();
        return image.GetUsedRect();
    }

    private static float GetFamilyBaseVisualScale(RevenantFamilyId family)
    {
        float ratio = family == RevenantFamilyId.Helen ? 0.8f : family == RevenantFamilyId.Skeleton ? 1.2f : 1f;
        // Retain the existing 204 px Osty baseline, measuring new idle art by alpha bounds.
        // All authored action frames share this scale; never resize by individual poses.
        return 204f * ratio / Math.Max(1, GetFamilyIdleBounds(family).Size.Y);
    }

    private static float GetFamilyBottomOffset(RevenantFamilyId family) =>
        GetFamilyIdleBounds(family).End.Y - 256f;

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
        PlayFamilyFrameAnimation("Attack");
    }

    public void PlayFamilyHitAnimation()
    {
        PlayFamilyFrameAnimation("Hit");
    }

    private void PlayFamilyFrameAnimation(string trigger)
    {
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual) ||
            _familyAnimation == null || !GodotObject.IsInstanceValid(_familyAnimation) ||
            CurrentFamilyId is not RevenantFamilyId family || _familyCreature?.IsAlive != true)
            return;
        StopFamilyTweens();
        _familyVisual.Position = GetFamilyVisualBasePosition(family);
        _familyVisual.Rotation = 0f;
        _familyAnimation.Call("play_trigger", trigger);
    }

    private void StopFamilyTweens()
    {
        _familyIdleTween?.Kill();
        _familyIdleTween = null;
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
                new AbstractIntent[] { new RevenantFamilyAttackIntent(3) },
            (RevenantFamilyId.Helen, RevenantFamilyAction.Second) =>
                new AbstractIntent[] { new RevenantFamilyAttackIntent(3), new BuffIntent() },
            (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new RevenantFamilyAttackIntent(5), new DebuffIntent() },
            (RevenantFamilyId.PumpkinHead, RevenantFamilyAction.Second) =>
                new AbstractIntent[] { new RevenantFamilyAttackIntent(5, 2) },
            (RevenantFamilyId.Skeleton, RevenantFamilyAction.First) =>
                new AbstractIntent[] { new RevenantFamilyAttackIntent(2), new DebuffIntent() },
            _ => new AbstractIntent[] { new RevenantFamilyAttackIntent(6) },
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
        AbstractIntent attackIntent = new RevenantFamilyAttackIntent(necro.DamagePerHit, necro.HitCount, powered: true);
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
