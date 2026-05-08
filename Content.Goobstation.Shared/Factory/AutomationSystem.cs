// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Factory.Slots;
using Content.Shared.Prototypes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Goobstation.Shared.Factory;

public sealed class AutomationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityQuery<AutomationSlotsComponent> _slotsQuery = default!;

    private List<EntProtoId> _automatable = new();
    /// <summary>
    /// All entities with <see cref="AutomationSlotsComponent"/>, maintained on prototype reload.
    /// </summary>
    public IReadOnlyList<EntProtoId> Automatable => _automatable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutomationSlotsComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<AutomationSlotsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutomationSlotsComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PhysicsComponent, AnchorStateChangedEvent>(OnAnchorChanged);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        CacheEntities();
    }

    private void OnInit(Entity<AutomationSlotsComponent> ent, ref ComponentInit args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            slot.Owner = ent;
            slot.Initialize();
        }
    }

    private void OnMapInit(Entity<AutomationSlotsComponent> ent, ref MapInitEvent args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            slot.AddPorts();
        }
    }

    private void OnShutdown(Entity<AutomationSlotsComponent> ent, ref ComponentShutdown args)
    {
        // don't care if the entity is being deleted
        if (TerminatingOrDeleted(ent))
            return;

        foreach (var slot in ent.Comp.Slots)
        {
            slot.RemovePorts();
        }
    }

    private void OnAnchorChanged(Entity<PhysicsComponent> ent, ref AnchorStateChangedEvent args)
    {
        // force collision events so machines can react to objects getting unanchored
        // should get reset after a tick due to collision wake
        if (!args.Anchored)
            _physics.WakeBody(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        CacheEntities();
    }

    private void CacheEntities()
    {
        _automatable.Clear();
        var name = Factory.GetComponentName<AutomationSlotsComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Components.ContainsKey(name))
                _automatable.Add(proto.ID);
        }

        _automatable.Sort();
    }

    #region Public API

    public AutomationSlot? GetSlot(Entity<AutomationSlotsComponent?> ent, string port, bool input)
    {
        // entity has no automation slots to begin with
        if (!_slotsQuery.Resolve(ent, ref ent.Comp, false))
            return null;

        foreach (var slot in ent.Comp.Slots)
        {
            string? id = input ? slot.Input : slot.Output;
            if (id == port)
                return slot;
        }

        return null;
    }

    public bool HasSlot(Entity<AutomationSlotsComponent?> ent, string port, bool input)
    {
        return GetSlot(ent, port, input) != null;
    }

    #endregion
}
