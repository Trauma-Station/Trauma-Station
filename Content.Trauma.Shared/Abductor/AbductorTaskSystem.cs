// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Abductor;

/// <summary>
/// Gives abductor tasks to mobs when <see cref="AbductorSubjectComponent"/> is added.
/// Provides API for working with it.
/// </summary>
public sealed class AbductorTaskSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private EntityQuery<AbductorSubjectComponent> _query = default!;

    // min-max random tasks to add
    // gland task is always added after this
    public const int MinTasks = 3;
    public const int MaxTasks = 6;

    public static readonly ProtoId<AbductorTaskPrototype> FinalTask = "InstallGland";

    /// <summary>
    /// Every task that can be rolled.
    /// </summary>
    public readonly List<AbductorTaskPrototype> AllTasks = new();
    private List<AbductorTaskPrototype> _validTasks = new();

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<AbductorSubjectComponent>();

        SubscribeLocalEvent<AbductorSubjectComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnMapInit(Entity<AbductorSubjectComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Tasks.Count > 0)
            return;

        var count = _random.Next(MinTasks, MaxTasks);
        ent.Comp.Tasks = PickTasks(ent, count);
        ent.Comp.Tasks.Add(FinalTask);
        DirtyField(ent, ent.Comp, nameof(AbductorSubjectComponent.Tasks));
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<AbductorTaskPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllTasks.Clear();
        foreach (var task in _proto.EnumeratePrototypes<AbductorTaskPrototype>())
        {
            if (task.Random)
                AllTasks.Add(task);
        }
    }

    #region Public API

    /// <summary>
    /// Pick a number of random tasks for a subject.
    /// Will only give at most <c>count</c> tasks, and never give duplicates.
    /// </summary>
    public List<ProtoId<AbductorTaskPrototype>> PickTasks(EntityUid target, int count)
    {
        _validTasks.Clear();
        foreach (var task in AllTasks)
        {
            if (!_random.Prob(task.Chance))
                continue;

            if (_conditions.TryConditions(target, task.Valid))
                _validTasks.Add(task);
        }

        var picked = new List<ProtoId<AbductorTaskPrototype>>();
        while (_validTasks.Count > 0 && picked.Count < count)
        {
            picked.Add(_random.PickAndTake(_validTasks).ID);
        }
        return picked;
    }

    /// <summary>
    /// Returns true if a task is currently complete for a subject.
    /// </summary>
    public bool IsTaskComplete(EntityUid target, [ForbidLiteral] ProtoId<AbductorTaskPrototype> id)
    {
        var task = _proto.Index(id);
        return task.Completed is {} completed
            ? _conditions.TryConditions(target, completed)
            : !_conditions.TryConditions(target, task.Valid);
    }

    /// <summary>
    /// Returns true if an entity is a subject of abductor experiments.
    /// </summary>
    public bool IsSubject(EntityUid target)
        => _query.HasComp(target);

    /// <summary>
    /// Handles completing the current task for a subject.
    /// Returns true if the current task exists and is now complete.
    /// </summary>
    public bool TryCompleteTask(Entity<AbductorSubjectComponent?> target)
    {
        if (!_query.Resolve(target, ref target.Comp, false) ||
            target.Comp.AllCompleted ||
            !IsTaskComplete(target, target.Comp.NextTask))
            return false;

        target.Comp.CompletedCount++;
        DirtyField(target, target.Comp, nameof(AbductorSubjectComponent.CompletedCount));
        return true;
    }

    /// <summary>
    /// Returns true if a target has all of its tasks completed.
    /// </summary>
    public bool AllTasksCompleted(Entity<AbductorSubjectComponent?> target)
        => _query.Resolve(target, ref target.Comp, false)
            ? target.Comp.AllCompleted
            : false;

    #endregion
}
