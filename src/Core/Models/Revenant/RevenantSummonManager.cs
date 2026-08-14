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
using MegaCrit.Sts2.Core.ValueProps;

namespace sts2mod.Core.Models.Revenant;

public enum RevenantFamilyId
{
    Helen,
    PumpkinHead,
    Skeleton,
}

public sealed class RevenantFamilyState
{
    // TODO_REVENANT_FAMILY_HP: Osty's one-point summon amount is the shared
    // Temporary shared family HP until final balance values are supplied.
    public bool IsAlive { get; set; } = true;
    public int CurrentHp { get; set; } = 1;
    public int MaxHp { get; set; } = 1;
    public int RetainedBlock { get; set; }
}

public sealed class RevenantNecro
{
    public required string MonsterId { get; init; }
    public required string MonsterType { get; init; }
    public required string SourceVisualsPath { get; init; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; init; }
    public bool IsAlive { get; set; } = true;
    public Sprite2D VisualNode { get; set; }

    public Task PerformAction(PlayerChoiceContext choiceContext)
    {
        // TODO_REVENANT_NECRO_ACTION: Per-monster action tables are intentionally absent.
        return Task.CompletedTask;
    }
}

public sealed class RevenantSummonManager
{
    private static readonly Dictionary<Player, RevenantSummonManager> Managers = new();
    private readonly Dictionary<RevenantFamilyId, RevenantFamilyState> _families =
        Enum.GetValues<RevenantFamilyId>().ToDictionary(id => id, _ => new RevenantFamilyState());
    private readonly List<RevenantNecro> _necros = new();
    private readonly HashSet<Creature> _convertedEnemies = new();
    private Sprite2D _familyVisual;

    private RevenantSummonManager(Player owner) => Owner = owner;

    public Player Owner { get; }
    public RevenantFamilyId? CurrentFamilyId { get; private set; }

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

    public RevenantFamilyState GetCurrentFamily()
    {
        SnapshotCurrentFamily();
        return CurrentFamilyId is RevenantFamilyId id ? _families[id] : null;
    }

    public IReadOnlyList<RevenantFamilyId> GetCallableFamilies()
    {
        SnapshotCurrentFamily();
        return _families
            .Where(pair => pair.Value.IsAlive && pair.Key != CurrentFamilyId)
            .Select(pair => pair.Key)
            .ToArray();
    }

    public async Task CallFamily(PlayerChoiceContext context, RevenantFamilyId family) =>
        await SwitchFamily(context, family);

    public async Task SwitchFamily(PlayerChoiceContext context, RevenantFamilyId family)
    {
        if (CurrentFamilyId == family || !_families[family].IsAlive)
            return;

        SnapshotCurrentFamily();
        Creature pet = Owner.Osty;
        if (pet == null || !pet.IsAlive)
        {
            await OstyCmd.Summon(context, Owner, 1m, context.LastInvolvedModel);
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
        RefreshFamilyVisual(family);
    }

    private void SnapshotCurrentFamily()
    {
        if (CurrentFamilyId is not RevenantFamilyId id)
            return;
        Creature pet = Owner.Osty;
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

    public async Task TriggerFamilyNormalAction(PlayerChoiceContext context)
    {
        if (CurrentFamilyId is not RevenantFamilyId family || Owner.Osty is not { IsAlive: true })
            return;
        bool first = Owner.RunState.Rng.Niche.NextBool();
        await PerformFamilyAction(context, family, first, false);
        SnapshotCurrentFamily();
    }

    public async Task TriggerFamilyStrongAction(PlayerChoiceContext context)
    {
        if (CurrentFamilyId is not RevenantFamilyId family || Owner.Osty is not { IsAlive: true })
            return;
        await PerformFamilyAction(context, family, true, true);
        SnapshotCurrentFamily();
    }

    private async Task PerformFamilyAction(
        PlayerChoiceContext context,
        RevenantFamilyId family,
        bool first,
        bool strong)
    {
        Creature pet = Owner.Osty;
        Creature[] enemies = Owner.Creature.CombatState.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToArray();

        Creature RandomEnemy() => enemies.Length == 0
            ? null
            : Owner.RunState.Rng.CombatTargets.NextItem(enemies);

        switch (family)
        {
            case RevenantFamilyId.Helen:
                if (strong)
                {
                    await CardPileCmd.Draw(context, 2m, Owner);
                    await PlayerCmd.GainEnergy(1m, Owner);
                }
                else if (first)
                {
                    Creature target = RandomEnemy();
                    if (target != null)
                        await CreatureCmd.Damage(context, target, 4m, ValueProp.Unpowered, pet, null);
                    await CardPileCmd.Draw(context, 1m, Owner);
                }
                else
                {
                    await CreatureCmd.GainBlock(Owner.Creature, 3m, ValueProp.Unpowered, null);
                    await PlayerCmd.GainEnergy(1m, Owner);
                }
                break;
            case RevenantFamilyId.PumpkinHead:
                Creature pumpkinTarget = RandomEnemy();
                if (pumpkinTarget == null)
                    break;
                if (strong)
                {
                    await CreatureCmd.Damage(context, pumpkinTarget, 27m, ValueProp.Unpowered, pet, null);
                }
                else if (first)
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
                if (strong)
                {
                    for (int i = 0; i < 4; i++)
                        await CreatureCmd.Damage(context, enemies.Where(e => e.IsAlive), 5m, ValueProp.Unpowered, pet, null);
                }
                else if (first)
                {
                    await CreatureCmd.GainBlock(pet, 8m, ValueProp.Unpowered, null);
                    await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Unpowered, null);
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
        CreateNecroVisual(necro);
    }
    public void RemoveNecro(RevenantNecro necro)
    {
        necro.IsAlive = false;
        necro.VisualNode?.QueueFree();
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
        await TriggerFamilyStrongAction(context);
        await TriggerAllNecros(context);
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
        string visualPath = AccessTools.Property(enemy.Monster.GetType(), "VisualsPath")
            ?.GetValue(enemy.Monster) as string ?? string.Empty;
        RegisterNecro(new RevenantNecro
        {
            MonsterId = enemy.Monster.Id.ToString(),
            MonsterType = enemy.Monster.GetType().FullName ?? enemy.Monster.GetType().Name,
            SourceVisualsPath = visualPath, // TODO_REVENANT_NECRO_VISUAL: reuse source with summon tint later.
            CurrentHp = enemy.MaxHp,
            MaxHp = enemy.MaxHp,
        });
    }

    public void CleanupVisuals()
    {
        _familyVisual?.QueueFree();
        _familyVisual = null;
        foreach (RevenantNecro necro in _necros)
            necro.VisualNode?.QueueFree();
    }

    private void RefreshFamilyVisual(RevenantFamilyId family)
    {
        NCreature petNode = FindCreatureNode(Owner.Osty);
        if (petNode == null)
            return;
        if (petNode.Body != null)
            petNode.Body.Visible = false;
        if (_familyVisual == null || !GodotObject.IsInstanceValid(_familyVisual))
        {
            _familyVisual = new Sprite2D
            {
                Name = "RevenantFamilyVisual",
                Position = new Vector2(0, -110),
                Scale = Vector2.One * 0.38f,
            };
            petNode.AddChild(_familyVisual);
        }
        string file = family switch
        {
            RevenantFamilyId.Helen => "helen.png",
            RevenantFamilyId.PumpkinHead => "frederick.png",
            _ => "sebastian.png",
        };
        _familyVisual.Texture = PreloadManager.Cache.GetTexture2D($"res://revenant_assets/families/{file}");
    }

    private void CreateNecroVisual(RevenantNecro necro)
    {
        NCreature playerNode = FindCreatureNode(Owner.Creature);
        if (playerNode?.GetParent() is not Node2D parent)
            return;
        var sprite = new Sprite2D
        {
            Name = $"RevenantNecroVisual{_necros.Count}",
            Texture = PreloadManager.Cache.GetTexture2D("res://revenant_assets/families/necro.png"),
            Modulate = new Color(0.72f, 0.55f, 0.95f, 0.62f),
            Scale = Vector2.One * 0.28f,
            GlobalPosition = playerNode.GlobalPosition + new Vector2(150f + 72f * (_necros.Count - 1), -105f),
        };
        parent.AddChild(sprite);
        necro.VisualNode = sprite; // TODO_REVENANT_NECRO_VISUAL: replace with safe source visuals/tint.
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
