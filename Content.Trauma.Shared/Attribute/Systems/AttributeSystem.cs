// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Attribute.Systems;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Attribute.Components;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// This handles all attribute related entities.
/// </summary>
public sealed partial class SharedAttributeSystem : CommonAttributeSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityQuery<AwakeMobComponent> _awakeQuery = default!;
    [Dependency] private readonly EntityQuery<AttributeComponent> _query = default!;
    [Dependency] private readonly EntityQuery<AttributeContainerComponent> _containerQuery = default!;
    [Dependency] private readonly EntityQuery<AttributeHolderComponent> _holderQuery = default!;

    /// <summary>
    /// Every attribute prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, AttributeComponent> AllAttributes = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeContainerComponent, ComponentStartup>(OnContainerStartup);
        SubscribeLocalEvent<AttributeContainerComponent, ComponentShutdown>(OnContainerShutdown);
        SubscribeLocalEvent<AttributeContainerComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<AttributeContainerComponent, OrganGotRemovedEvent>(OnOrganRemoved);
        SubscribeLocalEvent<AttributeContainerComponent, BorgBrainInsertedEvent>(OnBorgBrainInserted);
        SubscribeLocalEvent<AttributeContainerComponent, BorgBrainRemovedEvent>(OnBorgBrainRemoved);

        SubscribeLocalEvent<AttributeHolderComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<AttributeHolderComponent, OnAttributeSingleContest>(OnSingleContest);
        SubscribeLocalEvent<AttributeHolderComponent, OnAttributeOpposedContest>(OnOpposedContest);

        LoadAttributePrototypes();
    }

    private void OnContainerStartup(Entity<AttributeContainerComponent> ent, ref ComponentStartup args)
    {
        EnsureContainer(ent);
    }

    private void OnContainerShutdown(Entity<AttributeContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Container is { } container)
            _container.ShutdownContainer(container);
    }

    private void LinkContainer(EntityUid target, Entity<AttributeContainerComponent> ent)
    {
        // its all networked
        if (_timing.ApplyingState)
            return;

        var holder = EnsureComp<AttributeHolderComponent>(target);
        if (holder.AttributeEntity == ent.Owner)
            return; // no change

        DebugTools.Assert(ent.Comp.Holder == null,
            $"Tried to link {ToPrettyString(target)} to {ToPrettyString(ent)} but it was already linked to another holder {ToPrettyString(ent.Comp.Holder)}!");
        DebugTools.Assert(holder.AttributeEntity == null,
            $"Tried to link {ToPrettyString(target)} to {ToPrettyString(ent)} but it was already linked to another container {ToPrettyString(holder.AttributeEntity)}!");

        holder.AttributeEntity = ent;
        Dirty(target, holder);
        ent.Comp.Holder = target;
        DirtyField(ent, ent.Comp, nameof(AttributeContainerComponent.Holder));
    }

    private void UnlinkContainer(EntityUid target, Entity<AttributeContainerComponent> ent)
    {
        // its all networked
        if (_timing.ApplyingState ||
            !_holderQuery.TryComp(target, out var holder) ||
            holder.AttributeEntity == null) // already unlinked
            return;

        DebugTools.Assert(ent.Comp.Holder == target,
            $"Tried to unlink {ToPrettyString(target)} from {ToPrettyString(ent)} but it was linked to a different holder {ToPrettyString(ent.Comp.Holder)}!");
        DebugTools.Assert(holder.AttributeEntity == ent.Owner,
            $"Tried to unlink {ToPrettyString(target)} from {ToPrettyString(ent)} but it was linked to a different container {ToPrettyString(holder.AttributeEntity)}!");

        holder.AttributeEntity = null;
        Dirty(target, holder);
        ent.Comp.Holder = null;
        DirtyField(ent, ent.Comp, nameof(AttributeContainerComponent.Holder));
    }

    private void OnOrganInserted(Entity<AttributeContainerComponent> ent, ref OrganGotInsertedEvent args)
    {
        LinkContainer(args.Target, ent);
    }

    private void OnOrganRemoved(Entity<AttributeContainerComponent> ent, ref OrganGotRemovedEvent args)
    {
        UnlinkContainer(args.Target, ent);
    }

    private void OnBorgBrainInserted(Entity<AttributeContainerComponent> ent, ref BorgBrainInsertedEvent args)
    {
        LinkContainer(args.Chassis, ent);
    }

    private void OnBorgBrainRemoved(Entity<AttributeContainerComponent> ent, ref BorgBrainRemovedEvent args)
    {
        UnlinkContainer(args.Chassis, ent);
    }

    private void OnMindAdded(Entity<AttributeHolderComponent> ent, ref MindAddedMessage args)
    {
        // all player-controlled mobs have attributes
        EnsureAttributeContainer(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadAttributePrototypes();
    }

    private void LoadAttributePrototypes()
    {
        AllAttributes.Clear();
        var name = Factory.GetComponentName<AttributeComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            // TODO: replace with TryComp after engine update
            if (!proto.TryGetComponent<AttributeComponent>(name, out var comp))
                continue;

            AllAttributes[proto.ID] = comp;
        }
    }

    /// <summary>
    /// Increase an attribute unit's level for a target entity.
    /// This sets the level to max(current, new), NOT adding.
    /// If it does not exist it will be created.
    /// </summary>
    /// <returns>
    /// Null if spawning it fails.
    /// </returns>
    public Entity<AttributeComponent>? EnsureAttribute(Entity<AttributeContainerComponent> ent, [ForbidLiteral] EntProtoId id, FixedPoint2 value)
    {
        if (GetAttribute(ent, id) is { } existing)
        {
            if (existing.Comp.Inherent < value)
            {
                existing.Comp.Inherent = value;
                Dirty(existing, existing.Comp);
            }
            return existing;
        }

        PredictedTrySpawnInContainer(id, ent.Owner, AttributeContainerComponent.ContainerId, out var spawned);
        if (spawned is not { } unit)
        {
            Log.Error($"Failed to spawn attribute {id} for {ToPrettyString(ent)}!");
            return null;
        }

        var comp = _query.Comp(unit);
        comp.Inherent = value;
        Dirty(unit, comp);

        ent.Comp.AttributeDict[id] = unit;
        DirtyField(ent, ent.Comp, nameof(AttributeContainerComponent.AttributeDict));

        if (ent.Comp.Holder is not { } holder)
            return (unit, comp); // added attribute to a loose brain...

        var ev = new AttributeAddedEvent(ent, holder);
        RaiseLocalEvent(unit, ref ev);

        return (unit, comp);
    }

    /// <summary>
    /// Adds a list of attribute units to a attribute container.
    /// </summary>
    public void AddAttributeUnits(EntityUid target, Dictionary<EntProtoId, int> attributeList)
    {
        if (GetContainer(target) is not { } ent)
            return;

        foreach (var (id, level) in attributeList)
        {
            EnsureAttribute(ent, id, level);
        }
    }

    /// <summary>
    /// Removes a attribute unit from a container. Will not remove an attribute unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public EntityUid? RemoveAttribute(EntityUid target, [ForbidLiteral] EntProtoId id, bool force = false)
    {
        if (GetContainer(target) is not { } ent ||
            ent.Comp.Holder is not { } holder ||
            GetAttribute(ent, id) is not { } unit ||
            unit.Comp.Unremoveable && !force)
            return null;

        ent.Comp.AttributeDict.Remove(id);
        DirtyField(ent, ent.Comp, nameof(AttributeContainerComponent.AttributeDict));

        var ev = new AttributeRemovedEvent(ent, holder);
        RaiseLocalEvent(unit, ref ev);

        PredictedQueueDel(unit);
        return target;
    }

    /// <summary>
    /// Gets a attribute unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// Null if the target is not an attribute container, or if attribute unit wasn't found.
    /// </returns>
    public override Entity<AttributeComponent>? GetAttribute(EntityUid target, [ForbidLiteral] EntProtoId id)
        => GetContainer(target) is { } ent
            ? GetAttribute(ent, id)
            : null;

    public Entity<AttributeComponent>? GetAttribute(Entity<AttributeContainerComponent> ent, [ForbidLiteral] EntProtoId id)
        => ent.Comp.AttributeDict.TryGetValue(id, out var unit) && _query.TryComp(unit, out var comp)
            ? (unit, comp)
            : null;

    /// <summary>
    /// Returns all attribute units inside the container component.
    /// </summary>
    public List<Entity<AttributeComponent>>? TryGetAllAttributeUnits(EntityUid target)
    {
        if (GetContainer(target) is not { } ent)
            return null;

        var found = new List<Entity<AttributeComponent>>();
        foreach (var unit in ent.Comp.AttributeDict.Values)
        {
            if (_query.TryComp(unit, out var comp))
                found.Add((unit, comp));
        }

        return found;
    }

    /// <summary>
    /// Returns the first attribute entity of the target that has a given component.
    /// </summary>
    public EntityUid? HasAttributeComp<T>(EntityUid target) where T : IComponent
    {
        if (GetContainer(target)?.Comp.Container is not { } container)
            return null;

        var query = GetEntityQuery<T>();
        foreach (var attribute in container.ContainedEntities)
        {
            if (query.HasComp(attribute))
                return target;
        }

        return null;
    }

    /// <summary>
    /// Returns all attribute entities that have a required component.
    /// </summary>
    public List<Entity<T, AttributeComponent>>? GetAttributeWith<T>(EntityUid target) where T : IComponent
    {
        if (GetContainer(target)?.Comp.Container is not { } container)
            return null;

        var attributeEnts = new List<Entity<T, AttributeComponent>>();
        var query = GetEntityQuery<T>();
        foreach (var attribute in container.ContainedEntities)
        {
            if (!_query.TryComp(attribute, out var attributeComp))
                continue;

            if (query.TryComp(attribute, out var comp))
                attributeEnts.Add((attribute, comp, attributeComp));
        }

        return attributeEnts;
    }

    /// <summary>
    /// Returns true if an entity is a attribute holder, regardless of having a container set.
    /// </summary>
    public bool IsHolder(EntityUid target)
        => _holderQuery.HasComp(target);

    public override void ClearAttribute(EntityUid target, bool deleteAll)
    {
        if (GetContainer(target) is not { } ent)
            return;

        ent.Comp.AttributeDict.Clear();
        DirtyField(ent, ent.Comp, nameof(AttributeContainerComponent.AttributeDict));
        if (deleteAll && ent.Comp.Container is { } container)
        {
            foreach (var entity in container.ContainedEntities)
            {
                PredictedQueueDel(entity);
            }
        }
    }

    /// <summary>
    /// Get the attribute container (brain) of a potential attribute holder (mob, borg, etc or a brain)
    /// </summary>
    public Entity<AttributeContainerComponent>? GetContainer(EntityUid uid)
    {
        // if called with a brain, return itself
        if (_containerQuery.TryComp(uid, out var comp))
            return (uid, comp);

        // otherwise try use the cached brain
        if (_holderQuery.CompOrNull(uid)?.AttributeEntity is not { } ent || TerminatingOrDeleted(ent))
            return null;

        if (_containerQuery.TryComp(ent, out var container))
            return (ent, container);

        Log.Error($"Attribute entity {ToPrettyString(ent)} of holder {ToPrettyString(uid)} did not have AttributeContainerComponent!");
        return null;
    }

    /// <summary>
    /// Relays an event to all attribute entities a mob has.
    /// Does nothing if the mob is asleep or crit/dead.
    /// </summary>
    public void RelayEvent<T>(Entity<AttributeHolderComponent> ent, ref T args) where T : notnull
    {
        if (!_awakeQuery.HasComp(ent) || GetContainer(ent)?.Comp.Container is not { } container)
            return;

        // TODO: Somehow pass synchornization into attribute.

        foreach (var unit in container.ContainedEntities)
        {
            RaiseLocalEvent(unit, ref args);
        }
    }

    /// <summary>
    /// Relays an event to all non-martial arts attributes a mob has.
    /// It also relays it to the active martial art, but not any inactive oens.
    /// </summary>
    public void RelayActiveEvent<T>(Entity<AttributeHolderComponent> ent, ref T args) where T : notnull
    {
        if (!_awakeQuery.HasComp(ent) || GetContainer(ent) is not { } brain || brain.Comp.Container is not { } container)
            return;

        foreach (var unit in container.ContainedEntities)
        {
            RaiseLocalEvent(unit, ref args);
        }
    }

    private Container EnsureContainer(Entity<AttributeContainerComponent> ent)
    {
        if (ent.Comp.Container != null)
            return ent.Comp.Container;

        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, AttributeContainerComponent.ContainerId);
        return ent.Comp.Container;
    }

    public Entity<AttributeContainerComponent> EnsureAttributeContainer(EntityUid uid)
    {
        EnsureComp<AttributeHolderComponent>(uid);
        if (GetContainer(uid) is { } brain)
            return brain;

        // if there's no brain store attribute on the mob itself
        var comp = EnsureComp<AttributeContainerComponent>(uid);
        LinkContainer(uid, (uid, comp));
        return (uid, comp);
    }

    public static int LerpCurve(FixedPoint2 input, FixedPoint2 minX, FixedPoint2 maxX, FixedPoint2 minY, FixedPoint2 maxY)
    {
        FixedPoint2 rawY = minY + (input - minX) * (maxY - minY) / (maxX - minX);

        return rawY.Int();
    }

    private void OnSingleContest(Entity<AttributeHolderComponent> ent, ref OnAttributeSingleContest args)
    {
        args.RaiseEvent(ent.Owner);
        var mod = args.GetMod();

        var rolled = RollContest(ent.Owner);
        args.CriticallySucceeded = (rolled == 20);
        args.CriticallyFailed = (rolled == 1);
        args.Failed = (rolled + mod <= args.Threshold);
        args.Rolled = rolled;
    }

    private void OnOpposedContest(Entity<AttributeHolderComponent> ent, ref OnAttributeOpposedContest args)
    {
        args.RaiseEvent(ent.Owner);
        args.RaiseEvent2(args.Opposer);
        var mod = args.GetMod();

        var rolled = RollContest(ent.Owner);
        var opposing = RollContest(args.Opposer);

        args.CriticallySucceededUser = (rolled == 20);
        args.CriticallyFailedUser = (rolled == 1);
        args.Failed = (rolled + mod.Item1 <= opposing + mod.Item2);
        args.CriticallySucceededOpposed = (opposing == 20);
        args.CriticallyFailedOpposed = (opposing == 1);
        args.RolledSelf = rolled;
        args.RolledOpposed = opposing;
    }

    private int RollContest(EntityUid uid)
    {
        return SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid)).Next(1, 20 + 1);
    }
}

/// <summary>
/// Raised on an attribute entity after it gets added to a container.
/// </summary>
[ByRefEvent]
public record struct AttributeAddedEvent(Entity<AttributeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on an attribute entity after it has been removed from a container, before deleting it.
/// </summary>
[ByRefEvent]
public record struct AttributeRemovedEvent(Entity<AttributeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on an active attribute entity just before deactivating it.
/// </summary>
[ByRefEvent]
public record struct AttributeEnabledEvent(Entity<AttributeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Raised on an active attribute entity just after activating it.
/// </summary>
[ByRefEvent]
public record struct AttributeDisabledEvent(Entity<AttributeContainerComponent> Container, EntityUid Holder);

/// <summary>
/// Event to try show a skill popup to the user.
/// Both networked and raised locally if predicted.
/// </summary>
[Serializable, NetSerializable]
public sealed class SkillPopupEvent(string popup) : EntityEventArgs
{
    public readonly string Popup = popup;
}
