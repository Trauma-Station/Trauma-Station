// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Server.Maps;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Checks that every map in a pool has the required areas in the map prototype.
/// This means it was mapped at least once, maybe it was removed but that seems rare for important areas.
/// </summary>
[TestFixture]
[Category("MapTests")]
public sealed class MapPoolTest : GameTest
{
    [Test]
    public async Task RequiredAreasMappedTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.EntMan;
        var proto = server.ProtoMan;
        var deps = entMan.EntitySysManager.DependencyCollection;
        var loader = entMan.System<MapLoaderSystem>();

        var options = DeserializationOptions.Default;
        await server.WaitAssertion(() =>
        {
            var ev = new BeforeEntityReadEvent();
            entMan.EventBus.RaiseEvent(EventSource.Local, ev);

            Assert.Multiple(() =>
            {
                var missing = new HashSet<EntProtoId>();
                foreach (var pool in proto.EnumeratePrototypes<GameMapPoolPrototype>())
                {
                    if (pool.RequiredAreas is not {} requiredAreas)
                        continue; // don't load anything for pools that dont need to be checked

                    foreach (var mapId in pool.Maps)
                    {
                        var map = proto.Index<GameMapPrototype>(mapId).MapPath;
                        if (!loader.TryReadFile(map, out var data))
                        {
                            Assert.Fail($"Failed to read {map}");
                            continue;
                        }

                        // parses the yml but doesn't spawn anything, not terribly slow
                        var reader = new EntityDeserializer(deps,
                            data,
                            options,
                            ev.RenamedPrototypes,
                            ev.DeletedPrototypes);

                        if (!reader.TryProcessData())
                        {
                            Assert.Fail($"Failed to process {map}");
                            continue;
                        }

                        missing.Clear();
                        foreach (var area in requiredAreas)
                        {
                            missing.Add(area);
                        }

                        // check that at least 1 grid maybe uses each required area
                        foreach (var gridId in reader.GridYamlIds)
                        {
                            var grid = reader.YamlEntities[gridId].Components;
                            if (!grid.TryGetValue("AreaGrid", out var comp))
                                continue; // outdated map?

                            if (!comp.TryGet<MappingDataNode>("areaMap", out var areaMap))
                                continue; // no areas

                            foreach (var node in areaMap.Values)
                            {
                                var area = ((ValueDataNode) node).Value;
                                missing.Remove(area);
                                if (missing.Count == 0)
                                    goto done;
                            }
                        }

                        Assert.That(missing, Is.Empty,
                            $"Map {mapId} ({map}) was missing these areas required by the pool: {string.Join(", ", missing)}");

                    done:
                        continue;
                    }
                }
            });
        });
    }
}
