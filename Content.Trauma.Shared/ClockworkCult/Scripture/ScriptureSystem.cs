// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Prototypes;
using Content.Trauma.Shared.ClockworkCult.Slab;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// This handles the Scripture system.
/// A scripture is a <see cref="EntityPrototype"/> that holds another entity as a "produced result".
/// Scriptures can produce a result by getting recited via an entity with <see cref="ClockworkSlabComponent"/>.
/// A scripture can hold actions, structures, and other entities.
/// </summary>
public sealed partial class ScriptureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    /// <summary>
    /// All entity prototypes with <see cref="ScriptureComponent"/>.
    /// </summary>
    [ViewVariables]
    public List<EntProtoId> AllScriptures = new();

    private EntityQuery<ScriptureComponent> _scriptureQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeBattery();

        _scriptureQuery = GetEntityQuery<ScriptureComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<ScriptureContainerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ScriptureContainerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScriptureContainerComponent, ScriptureReciteEvent>(OnRecite);

        LoadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        LoadPrototypes();
    }

    /// <summary>
    ///  Initialize the scripture container on the entity
    /// </summary>
    private void OnCompInit(Entity<ScriptureContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Scriptures = _container.EnsureContainer<Container>(ent, ScriptureContainerComponent.ContainerId);
    }

    /// <summary>
    ///  Clear the scripture container
    /// </summary>
    private void OnShutdown(Entity<ScriptureContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Scriptures is not { } container)
            return;

        _container.ShutdownContainer(container);
    }

    private void OnRecite(Entity<ScriptureContainerComponent> ent, ref ScriptureReciteEvent args)
    {
        if (!_proto.TryIndex(args.Scripture, out var scripture)
            || !scripture.TryGetComponent<ScriptureComponent>(out var scriptureComponent))
            return;

        // Raised on user to check if we have enough power to cast
        var user = args.Actor;
        var attemptEv = new ReciteAttemptEvent(scriptureComponent.PowerCost);
        RaiseLocalEvent(user, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        Log.Debug("The scripture got recited");
    }

    private void LoadPrototypes()
    {
        AllScriptures.Clear();
        var scripture = Factory.GetComponentName<ScriptureComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.Components.ContainsKey(scripture))
                continue;

            var id = proto.ID;
            AllScriptures.Add(id);
        }
    }

    #region Public Api

    /// <summary>
    /// Tries to add a scripture to an entity,
    /// ensures <see cref="ScriptureContainerComponent"/> if it doesn't exist on the target.
    /// </summary>
    /// <returns></returns>
    public bool TryAddScripture(EntityUid target, EntProtoId scriptureProto)
    {
        if (!CanAddScripture(scriptureProto))
            return false;

        EnsureComp<ScriptureContainerComponent>(target);

        if (!PredictedTrySpawnInContainer(scriptureProto, target, ScriptureContainerComponent.ContainerId, out _))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the scripture prototype from an entity with <see cref="ScriptureComponent"/>
    /// </summary>
    public EntProtoId? TryGetScripturePrototype(EntityUid scripture)
    {
        if (!_scriptureQuery.HasComp(scripture))
            return null;

        return Prototype(scripture)?.ID;
    }

    /// <summary>
    /// Gets a scripture <see cref="EntityUid"/> via its <see cref="EntityPrototype"/>, returns null if not found.
    /// </summary>
    public EntityUid? TryGetScripture(EntityUid uid, EntProtoId scripture, ScriptureContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.Scriptures is not {} scriptures)
            return null;

        foreach (var scriptEnt in scriptures.ContainedEntities)
        {
            if (TryGetScripturePrototype(scriptEnt) is not {} scriptProto)
                continue;

            if (scriptProto == scripture)
                return scriptEnt;
        }

        return null;
    }
    #endregion

    private bool CanAddScripture(EntProtoId scripture)
    {
        if (!_proto.Resolve(scripture, out var scriptureData))
            return false;

        if (!scriptureData.HasComponent<ScriptureComponent>())
            return false;

        return true;
    }
}

/// <summary>
/// Raised on the user to check if the recite can succeed (do we have enough power to cast?).
/// </summary>
[ByRefEvent]
public record struct ReciteAttemptEvent(int ScriptureCost, bool Cancelled = false);
