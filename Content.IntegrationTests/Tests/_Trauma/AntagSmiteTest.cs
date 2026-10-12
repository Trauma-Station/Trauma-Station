// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag.Components;
using Content.Trauma.Shared.Antag;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class AntagSmiteTest : GameTest
{
    /// <summary>
    /// Verifies that no antag smite's gamerule allows latejoins to roll its antag.
    /// </summary>
    [Test]
    public async Task AntagSmitesNoLatejoinTest()
    {
        var factory = SEntMan.ComponentFactory;
        var antagName = factory.CompName<AntagSelectionComponent>();
        var failed = new List<string>();
        foreach (var smite in SProtoMan.EnumeratePrototypes<AntagSmitePrototype>())
        {
            var rule = SProtoMan.Index(smite.Rule);
            if (!rule.TryComp<AntagSelectionComponent>(antagName, out var antag))
            {
                failed.Add($"Antag smite {smite.ID}'s gamerule {smite.Rule} was missing {antagName}Component!");
                continue;
            }

            if (antag.LateJoinAdditional)
                failed.Add($"Antag smite {smite.ID}'s gamerule {smite.Rule} must not allow latejoins, use lateJoinAdditional: false");
        }

        if (failed.Count > 0)
            Assert.Fail(string.Join("\n", failed));
    }
}
