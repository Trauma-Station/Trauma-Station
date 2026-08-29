// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Preferences.Loadouts;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class LoadoutsTest : GameTest
{
    /// <summary>
    /// Ensures that every <see cref="LoadoutPrototype"/> is present in at least 1 <see cref="LoadoutGroupPrototype"/>.
    /// It is assumed that every group is in some job or something.
    /// </summary>
    [Test]
    public async Task NoOrphanedLoadoutsTest()
    {
        // go through each group
        var grouped = new HashSet<ProtoId<LoadoutPrototype>>();
        foreach (var group in SProtoMan.EnumeratePrototypes<LoadoutGroupPrototype>())
        {
            // and collect the loadouts they all have
            foreach (var loadout in group.Loadouts)
            {
                grouped.Add(loadout);
            }
        }

        var orphans = new List<string>();
        // then go through each loadout
        foreach (var loadout in SProtoMan.EnumeratePrototypes<LoadoutPrototype>())
        {
            // and make sure it has a group
            var id = loadout.ID;
            if (!grouped.Contains(id))
                orphans.Add(id);
        }

        Assert.That(orphans, Is.Empty, $"Orphaned loadouts {string.Join(' ', orphans)} were not found in any LoadoutGroupPrototype, they cannot be used");
    }

    /// <summary>
    /// Makes sure that all loadout equipment items are clothing that can be placed in the set slot.
    /// </summary>
    [Test]
    public async Task LoadoutsEquipmentValidTest()
    {
        var clothingName = SEntMan.ComponentFactory.CompName<ClothingComponent>();
        var invalid = new List<string>();
        foreach (var loadout in SProtoMan.EnumeratePrototypes<LoadoutPrototype>())
        {
            foreach (var (slot, id) in loadout.Equipment)
            {
                var proto = SProtoMan.Index(id);
                if (!proto.TryComp<ClothingComponent>(clothingName, out var clothing))
                {
                    invalid.Add($"- {loadout.ID} - {id} in {slot} slot is missing ClothingComponent");
                    continue;
                }

                var mask = SProtoMan.Index(slot).Flags;
                if ((clothing.Slots & mask) == SlotFlags.NONE)
                {
                    invalid.Add($"- {loadout.ID} - {id} cannot be equipped to {slot} slot");
                    continue;
                }
            }
        }

        Assert.That(invalid.Count, Is.Zero, string.Join('\n', invalid));
    }
}
