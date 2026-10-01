// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Systems;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class ClankerTest : GameTest
{
    private static readonly ProtoId<InventorySlotPrototype> Ears = "ears";
    private static readonly ProtoId<SpeciesPrototype> Species = "IPC";

    [SidedDependency(Side.Server)] private SharedContainerSystem _container = default!;
    [SidedDependency(Side.Server)] private StationSpawningSystem _spawning = default!;

    /// <summary>
    /// Makes sure that IPCs start with the encryption keys of each job's headset.
    /// </summary>
    [Test]
    public async Task ClankerRadioTest()
    {
        var map = await Pair.CreateTestMap();
        var coords = map.GridCoords;
        var failed = new List<string>();
        var keyIds = new HashSet<EntProtoId>();
        await Server.WaitPost(() =>
        {
            var fillName = SEntMan.ComponentFactory.CompName<ContainerFillComponent>();
            var profile = new HumanoidCharacterProfile()
                .WithSpecies(Species);
            var containerId = EncryptionKeyHolderComponent.KeyContainerName;
            foreach (var job in SProtoMan.EnumeratePrototypes<JobPrototype>())
            {
                // skip AI and borgs obviously
                if (job.JobEntity != null ||
                    // ignore jobs that have no headset
                    !SProtoMan.TryIndex(job.StartingGear, out var gear) ||
                    !gear.Equipment.TryGetValue(Ears, out var id) ||
                    !SProtoMan.Resolve(id, out var proto) ||
                    !proto.TryComp<ContainerFillComponent>(fillName, out var fill) ||
                    !fill.Containers.TryGetValue(containerId, out var keys))
                    continue;

                var mob = _spawning.SpawnPlayerMob(coords, job.ID, profile, null);
                if (!SHasComp<EncryptionKeyHolderComponent>(mob))
                {
                    failed.Add($"{job.ID} - bad mob {SToPrettyString(mob)} was missing EncryptionKeyHolder!");
                    SDel(mob);
                    continue;
                }

                if (!_container.TryGetContainer(mob, containerId, out var container))
                {
                    failed.Add($"{job.ID} - missing {containerId} container on {SToPrettyString(mob)}!");
                    SDel(mob);
                    continue;
                }

                // check that every key in the headset's fill is present in the IPC container
                keyIds.Clear();
                foreach (var key in container.ContainedEntities)
                {
                    keyIds.Add(SPrototype(key)!.ID);
                }

                foreach (var keyId in keys)
                {
                    if (!keyIds.Contains(keyId))
                        failed.Add($"{job.ID} - missing key {keyId} from {id}");
                }
                SDel(mob);
            }
        });

        Assert.That(failed, Is.Empty, "IPC didn't get radio keys for every job");
    }
}
