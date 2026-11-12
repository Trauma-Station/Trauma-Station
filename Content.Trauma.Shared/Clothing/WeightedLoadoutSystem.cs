// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Systems;
using Content.Trauma.Common.Inventory;
using Content.Trauma.Shared.Clothing.Components;
using Content.Trauma.Shared.Clothing.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Clothing;

public sealed partial class WeightedLoadoutSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LoadoutSystem _loadout = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StationSpawningSystem _station = default!;

    /// <summary>
    /// An array of already sorted slots, for filling them up in order.
    /// This is required because some slots may need to be filled up first before others.
    /// </summary>
    private static readonly ProtoId<InventorySlotPrototype>[] SlotOrder =
    {
        "head", "eyes", "neck", "mask", "jumpsuit", "outerClothing", "back",
        "shoes", "belt", "id", "pocket1", "pocket2", "ears", "gloves", "suitstorage"
    };


    [SubscribeLocalEvent(after: [typeof(InitialBodySystem)])]
    private void OnMapInit(Entity<WeightedLoadoutComponent> ent, ref MapInitEvent args)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (ent.Comp.Loadout is { } loadout && ProtoMan.Resolve(loadout, out var loadoutPrototype))
        {
            var archetype = loadoutPrototype.Pick(random);
            ApplyArchetype(ent, archetype, random);
        }

        // Picking without anything will throw...
        if (ent.Comp.Weights.Count > 0)
        {
            var gear = random.Pick(ent.Comp.Weights);
            _station.EquipStartingGear(ent, gear);
        }

        _loadout.GearEquipped(ent);
    }

    /// <summary>
    /// Applies the loadout archetype on the entity
    /// </summary>
    /// <param name="uid">The entity to apply it to</param>
    /// <param name="proto">The loadout archetype</param>
    /// <param name="random"><see cref="IRobustRandom"/></param>
    private void ApplyArchetype(EntityUid uid, ProtoId<LoadoutArchetypePrototype> proto, IRobustRandom random)
    {
        if (!ProtoMan.Resolve(proto, out var archetype))
            return;

        if (archetype.Gear is { } gear)
            _station.EquipStartingGear(uid, gear);

        var ordered = archetype.Slots.OrderBy(kv => Array.IndexOf(SlotOrder, kv.Key));
        foreach (var (slot, pool) in ordered)
        {
            if (pool.Items.Count == 0 || !random.Prob(pool.Chance))
                continue;

            // We only fill slots that aren't filled already.
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                continue;

            var item = random.Pick(pool.Items);
            _inventory.SpawnItemInSlot(uid, slot, item, silent: true, force: true);
        }

        foreach (var pool in archetype.Hands)
        {
            if (pool.Items.Count == 0 || !random.Prob(pool.Chance))
                continue;

            var item = PredictedSpawnAtPosition(random.Pick(pool.Items), Transform(uid).Coordinates);
            _hands.PickupOrDrop(uid, item);
        }
    }
}
