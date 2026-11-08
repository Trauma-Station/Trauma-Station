// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Station.Systems;
using Content.Trauma.Shared.Clothing.Components;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Clothing;

public sealed partial class WeightedLoadoutSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LoadoutSystem _loadout = default!;
    [Dependency] private StationSpawningSystem _station = default!;

    // Wait until the character has all their organs before we give them their loadout
    [SubscribeLocalEvent(after: [typeof(InitialBodySystem)])]
    private void OnMapInit(Entity<WeightedLoadoutComponent> ent, ref MapInitEvent args)
    {
        if (TryPickWeighted(ent.Comp.Weights, kv => kv.Value, out var gear))
            _station.EquipStartingGear(ent, gear.Key);

        if (ent.Comp.Loadout is { } id && TryPickWeighted(_proto.Index(id).Archetypes.Values, a => a.Weight, out var archetype))
            ApplyArchetype(ent, archetype);

        _loadout.GearEquipped(ent);
    }

    private static readonly string[] SlotOrder =
    {
        "head", "eyes", "neck", "mask", "jumpsuit", "outerClothing", "back",
        "shoes", "belt", "id", "pocket1", "pocket2", "ears", "gloves", "suitstorage"
    };

    private void ApplyArchetype(EntityUid ent, LoadoutArchetype archetype)
    {
        if (archetype.Gear is { } gear)
            _station.EquipStartingGear(ent, gear);

        // dependent slots (pockets, belt, suitstorage) need their parent slot filled first
        var ordered = archetype.Slots.OrderBy(kv =>
        {
            var i = Array.IndexOf(SlotOrder, kv.Key);
            return i < 0 ? int.MaxValue : i;
        });

        foreach (var (slot, pool) in ordered)
        {
            if (pool.Chance < 1f && !_random.Prob(pool.Chance))
                continue;

            if (_inventory.TryGetSlotEntity(ent, slot, out _))
                continue;

            if (!TryPickWeighted(pool.Items, kv => kv.Value, out var item))
                continue;

            _inventory.SpawnItemInSlot(ent, slot, item.Key, silent: true, force: true);
        }
    }

    private bool TryPickWeighted<T>(IEnumerable<T> options, Func<T, float> getWeight, [MaybeNullWhen(false)] out T picked)
    {
        var total = 0f;
        foreach (var option in options)
        {
            var w = getWeight(option);
            if (w > 0f)
                total += w;
        }

        picked = default;
        if (total <= 0f)
            return false;

        var roll = _random.NextFloat() * total;
        foreach (var option in options)
        {
            var w = getWeight(option);
            if (w <= 0f)
                continue;

            picked = option;
            roll -= w;
            if (roll < 0f)
                return true;
        }

        return picked is not null; // float rounding fallback
    }
}
