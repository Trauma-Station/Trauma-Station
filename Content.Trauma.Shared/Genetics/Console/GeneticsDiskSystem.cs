// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Kitchen;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Console;

public sealed class GeneticsDiskSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    private EntityQuery<GeneticsDiskComponent> _query;
    private EntityQuery<GeneticsDiskSlotComponent> _slotQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<GeneticsDiskComponent>();
        _slotQuery = GetEntityQuery<GeneticsDiskSlotComponent>();

        SubscribeLocalEvent<GeneticsDiskComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(Entity<GeneticsDiskComponent> ent, ref BeingMicrowavedEvent args)
    {
        SetMutation(ent, null);
        SetEnzymes(ent, null);
    }

    /// <summary>
    /// Changes the mutation stored on a disk.
    /// </summary>
    public void SetMutation(Entity<GeneticsDiskComponent> ent, EntProtoId<MutationComponent>? id)
    {
        if (ent.Comp.Mutation == id)
            return;

        ent.Comp.Mutation = id;
        DirtyField(ent, ent.Comp, nameof(GeneticsDiskComponent.Mutation));

        if (id != null) // mutually exclusive
            SetEnzymes(ent, null);
    }

    /// <summary>
    /// Changes the unique enzymes stored on a disk.
    /// </summary>
    public void SetEnzymes(Entity<GeneticsDiskComponent> ent, UniqueEnzymes? enzymes)
    {
        if (ent.Comp.Enzymes?.Name == enzymes?.Name)
            return;

        ent.Comp.Enzymes = enzymes;
        DirtyField(ent, ent.Comp, nameof(GeneticsDiskComponent.Enzymes));

        if (enzymes != null) // mutually exclusive
            SetMutation(ent, null);
    }

    /// <summary>
    /// Gets the disk in a computer's disk slot, or null if it has no disk.
    /// </summary>
    public Entity<GeneticsDiskComponent>? GetDisk(Entity<GeneticsDiskSlotComponent?> ent)
    {
        if (_slotQuery.Resolve(ent, ref ent.Comp) &&
            _slots.GetItemOrNull(ent.Owner, ent.Comp.DiskSlot) is {} item &&
            _query.TryComp(item, out var disk))
            return (item, disk);

        return null;
    }
}
