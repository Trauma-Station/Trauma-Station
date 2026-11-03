// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Server.Biome;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Parallax;
using Content.Shared.EntityEffects;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Content.Trauma.Shared.Planet;

namespace Content.Trauma.Server.Station;

/// <summary>
/// System that runs a planet setup on maps that have a <see cref="PlanetPrototype"/> specified in their
/// <see cref="GameMapPrototype"/> definition.
///
/// This runs the setup after the game loads the map, unlike LL,
/// so you should use this for main playable maps, like Glacier.
/// </summary>
public sealed partial class StationPlanetSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private BiomeSystem _biome = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [Dependency] private EntityQuery<BiomeComponent> _biomeQuery = default!;

    [SubscribeLocalEvent]
    private void OnPostLoad(PostGameMapLoad args)
    {
        if (args.GameMap.PlanetProto is not { } planetProto)
            return;

        var map = _map.GetMap(args.Map);
        var proto = ProtoMan.Index<PlanetPrototype>(planetProto);
        var biome = ProtoMan.Index(proto.Biome);

        PlanetSetup(map, biome, proto);

        var loadBox = Box2.CentredAroundZero(new Vector2(proto.RestrictedRange * 2, proto.RestrictedRange * 2));

        // Essentially, optimizes the area from the restricted range, by not unloading already loaded chunks.
        // Required for any planet map where you want to hold many players
        var optimize = EnsureComp<BiomeOptimizeComponent>(map);
        optimize.LoadArea = loadBox;

        if (!_biomeQuery.TryComp(map, out var biomeComp))
            return;

        // Finally, preloads the biome so the server does not hate you
        _biome.Preload(map, biomeComp, loadBox);
    }

    /// <summary>
    /// Sets up a basic planet, based on it's <see cref="PlanetPrototype"/>
    /// </summary>
    /// <param name="map">The entity of the map</param>
    /// <param name="biome">The biome template</param>
    /// <param name="proto">The planet prototype</param>
    private void PlanetSetup(EntityUid map, BiomeTemplatePrototype biome, PlanetPrototype proto)
    {
        // Boilerplate with sane defaults (light cycle, biome, map light, etc.)
        _biome.EnsurePlanet(map, biome, mapLight: proto.MapLight);

        // Are you a raider?
        var restricted = EnsureComp<RestrictedRangeComponent>(map);
        restricted.Range = proto.RestrictedRange;

        // Marker Layers, usually for ores
        if (_biomeQuery.TryComp(map, out var biomeComp))
        {
            foreach (var marker in proto.BiomeMarkerLayers)
            {
                _biome.AddMarkerLayer(map, biomeComp, marker);
            }
        }

        // Atmosphere setup
        _atmos.SetMapAtmosphere(map, false, proto.Atmosphere);

        // Optional effects, like adding components, status effects etc
        if (proto.Effects is { } effects)
            _effects.ApplyEffects(map, effects);
    }
}
