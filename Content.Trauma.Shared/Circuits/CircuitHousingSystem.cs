// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Circuits;

/// <summary>
/// Controls power/insertion logic for circuit housings.
/// </summary>
public sealed partial class CircuitHousingSystem : EntitySystem
{
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private EntityQuery<CircuitComponent> _query = default!;
    [Dependency] private EntityQuery<PowerCellDrawComponent> _drawQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircuitHousingComponent, EntInsertedIntoContainerMessage>(OnCircuitInserted);
        SubscribeLocalEvent<CircuitHousingComponent, EntRemovedFromContainerMessage>(OnCircuitRemoved);
        SubscribeLocalEvent<CircuitHousingComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CircuitHousingComponent, PowerCellSlotEmptyEvent>(OnCellEmpty);
        SubscribeLocalEvent<CircuitHousingComponent, PowerCellChangedEvent>(OnCellChanged);
    }

    private void OnCircuitInserted(Entity<CircuitHousingComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId || !_query.TryComp(args.Entity, out var circuit))
            return;

        circuit.Housing = ent.Owner;
        Dirty(args.Entity, circuit);
        ent.Comp.Circuit = args.Entity;
        Dirty(ent);
        SetDrawing(ent, true);
        UpdateActive(ent);
    }

    private void OnCircuitRemoved(Entity<CircuitHousingComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId || !_query.TryComp(args.Entity, out var circuit))
            return;

        UpdateActive(ent, false); // stop updating it now that its removed
        circuit.Housing = null;
        Dirty(args.Entity, circuit);
        ent.Comp.Circuit = null;
        Dirty(ent);
        SetDrawing(ent, false);
    }

    private void OnPowerChanged(Entity<CircuitHousingComponent> ent, ref PowerChangedEvent args)
    {
        SetPowered(ent, args.Powered);
    }

    private void OnCellEmpty(Entity<CircuitHousingComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        SetPowered(ent, false);
    }

    private void OnCellChanged(Entity<CircuitHousingComponent> ent, ref PowerCellChangedEvent args)
    {
        SetPowered(ent, _cell.HasDrawCharge(ent.Owner));
    }

    private void SetDrawing(EntityUid uid, bool drawing)
    {
        if (_drawQuery.TryComp(uid, out var draw))
            _cell.SetDrawEnabled((uid, draw), drawing);
        else
            _power.SetPowerDisabled(uid, !drawing);
    }

    private void SetPowered(Entity<CircuitHousingComponent> ent, bool powered)
    {
        if (ent.Comp.Powered == powered)
            return;

        ent.Comp.Powered = powered;
        Dirty(ent);
        UpdateActive(ent);
    }

    private void UpdateActive(Entity<CircuitHousingComponent> ent, bool active = true)
    {
        if (ent.Comp.Circuit is not { } circuit)
            return;

        if (active && ent.Comp.Powered)
            EnsureComp<ActiveCircuitComponent>(circuit);
        else
            RemComp<ActiveCircuitComponent>(circuit);
    }
}
