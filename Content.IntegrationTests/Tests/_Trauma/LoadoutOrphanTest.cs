// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class LoadoutOrphanTest : GameTest
{
    /// <summary>
    /// Ensures that every <see cref="LoadoutPrototype"/> is present in at least 1 <see cref="LoadoutGroupPrototype"/>.
    /// It is assumed that every group is in some job or something.
    /// </summary>
    [Test]
    public async Task NoOrphanedLoadoutsTest()
    {
        var server = Pair.Server;
        var proto = server.ProtoMan;

        // go through each group
        var grouped = new HashSet<ProtoId<LoadoutPrototype>>();
        foreach (var group in proto.EnumeratePrototypes<LoadoutGroupPrototype>())
        {
            // and collect the loadouts they all have
            foreach (var loadout in group.Loadouts)
            {
                grouped.Add(loadout);
            }
        }

        await server.WaitAssertion(() =>
        {
            var orphans = new List<string>();
            // then go through each loadout
            foreach (var loadout in proto.EnumeratePrototypes<LoadoutPrototype>())
            {
                // and make sure it has a group
                var id = loadout.ID;
                if (!grouped.Contains(id))
                    orphans.Add(id);
            }

            Assert.That(orphans, Is.Empty, $"Orphaned loadouts {string.Join(' ', orphans)} were not found in any LoadoutGroupPrototype, they cannot be used");
        });
    }
}
