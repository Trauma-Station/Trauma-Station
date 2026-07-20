// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.Body;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Trauma.Shared.Body.Chips;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class BodyTest : GameTest
{
    public static EntProtoId Urist = "MobHuman";
    public static EntProtoId<OrganChipComponent> TestChip = "SkillChipLaser";
    public static ProtoId<PolymorphPrototype> HumanoidPolymorph = "Bananamen";

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private BodyPartSystem _part = default!;
    [SidedDependency(Side.Server)] private BodyRestoreSystem _restore = default!;
    [SidedDependency(Side.Server)] private OrganChipSystem _chip = default!;
    [SidedDependency(Side.Server)] private PolymorphSystem _polymorph = default!;

    /// <summary>
    /// Makes sure that every mob with a Body has a root part (torso).
    /// </summary>
    [Test]
    public async Task BodyRootPartExists()
    {
        var factory = SEntMan.ComponentFactory;

        var map = await Pair.CreateTestMap();

        var bodyName = factory.CompName<BodyComponent>();
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (Pair.IsTestPrototype(proto) || !proto.HasComp(bodyName))
                        continue;

                    var mob = SEntMan.SpawnEntity(proto.ID, map.GridCoords);
                    Assert.That(_part.GetRootPart(mob), Is.Not.Null, $"{SEntMan.ToPrettyString(mob)} had no root part!");
                    SEntMan.DeleteEntity(mob);
                }
            });
        });
    }

    /// <summary>
    /// Makes sure that every species mob can have all of its organs removed and restored, remaining the same.
    /// </summary>
    [Test]
    public async Task BodyRestoreTest()
    {
        var map = await Pair.CreateTestMap();

        var started = new HashSet<string>();
        var ended = new HashSet<string>();
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var species in SProtoMan.EnumeratePrototypes<SpeciesPrototype>())
                {
                    var proto = species.Prototype;
                    var mob = SEntMan.SpawnEntity(proto, map.GridCoords);
                    // get the starting list of organs
                    started.Clear();
                    foreach (var organ in _body.GetOrgans(mob))
                    {
                        started.Add(organ.Comp.Category);
                    }

                    // remove all non-root organs
                    foreach (var organ in _body.GetOrgans<ChildOrganComponent>(mob))
                    {
                        SEntMan.DeleteEntity(organ);
                    }

                    // restore them
                    _restore.RestoreBody(mob);

                    // get the new list of organs
                    ended.Clear();
                    foreach (var organ in _body.GetOrgans(mob))
                    {
                        ended.Add(organ.Comp.Category);
                    }

                    // make sure they are the same, or some organs were lost in the cycle
                    Assert.That(ended, Is.EquivalentTo(started),
                        $"{SEntMan.ToPrettyString(mob)} had different organs after having its body restored!");

                    SEntMan.DeleteEntity(mob);
                }
            });
        });
    }

    /// <summary>
    /// For every species, collects every marking layer its bodyparts support.
    /// Then checks that every marking's layer is present from its marking group species.
    /// Prevents e.g. moth wings marking using Wings layer but no part having it so you just get no wings.
    /// </summary>
    [Test]
    public async Task BodyMarkingsTest()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var validLayers = new Dictionary<ProtoId<MarkingsGroupPrototype>, HashSet<HumanoidVisualLayers>>();

                // first collect the marking groups every species' parts has
                foreach (var species in SProtoMan.EnumeratePrototypes<SpeciesPrototype>())
                {
                    if (Pair.IsTestPrototype(species))
                        continue;

                    var mob = SEntMan.SpawnEntity(species.Prototype, map.GridCoords);
                    foreach (var organ in _body.GetOrgans<VisualOrganMarkingsComponent>(mob))
                    {
                        var group = organ.Comp.MarkingData.Group;
                        var layers = organ.Comp.MarkingData.Layers;
                        if (!validLayers.TryGetValue(group, out var groupLayers))
                            validLayers[group] = groupLayers = new();
                        groupLayers.UnionWith(layers);
                    }
                    SEntMan.DeleteEntity(mob);
                }

                // then make sure every marking has a part to be added to
                var errors = new List<string>();
                foreach (var marking in SProtoMan.EnumeratePrototypes<MarkingPrototype>())
                {
                    if (Pair.IsTestPrototype(marking) || marking.GroupWhitelist is not {} groups)
                        continue; // not whitelisted, assumed that it will work on anything?

                    var layer = marking.BodyPart;
                    foreach (var group in groups)
                    {
                        if (!validLayers.TryGetValue(group, out var layers))
                        {
                            errors.Add($"Marking {marking.ID} is whitelisted for group {group} which has no parts?!");
                            continue;
                        }

                        if (!layers.Contains(layer))
                            errors.Add($"Marking {marking.ID} is whitelisted for group {group} which is missing a part for layer {layer}!");
                    }
                }

                // print any errors and fail once instead of having 50 identical stack traces
                if (errors.Count > 0)
                    Assert.Fail(string.Join("\n", errors));
            });
        });
    }

    /// <summary>
    /// Makes sure that organ chips persist when polymorphing to another humanoid mob.
    /// </summary>
    [Test]
    public async Task ChipsPolymorphTest()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var urist = SEntMan.SpawnEntity(Urist, map.GridCoords);
            Assert.That(CountChips(urist), Is.EqualTo(0), "Fresh urist shouldnt have skillchips");
            _chip.InstallChip(urist, TestChip);
            Assert.That(CountChips(urist), Is.EqualTo(1), "Urist should have gained a skillchip after installing it");

            if (_polymorph.PolymorphEntity(urist, HumanoidPolymorph) is not { } nana)
            {
                Assert.Fail($"Failed to polymorph {SEntMan.ToPrettyString(urist)} into {HumanoidPolymorph}!");
                return;
            }

            Assert.That(CountChips(urist), Is.EqualTo(0), "Urist shouldnt have skillchips after being polymorphed");
            Assert.That(CountChips(nana), Is.EqualTo(1), "Banana should have transferred urist's skillchip from polymorphing");
            SEntMan.DeleteEntity(nana);
            SEntMan.DeleteEntity(urist);
        });
    }

    private int CountChips(EntityUid mob)
    {
        var count = 0;
        foreach (var organ in _body.GetOrgans<OrganChipContainerComponent>(mob))
        {
            count += organ.Comp.Container.Count;
        }
        return count;
    }

    // TODO: more stuff!
}
