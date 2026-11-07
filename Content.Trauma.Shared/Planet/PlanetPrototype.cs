// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Procedural.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;

namespace Content.Trauma.Shared.Planet;

/// <summary>
/// Prototype used for game maps that are planets, like Glacier
/// </summary>
[Prototype]
public sealed partial class PlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// The biome to create the planet with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Ambient lighting for the map.
    /// </summary>
    [DataField]
    public Color MapLight = Color.FromHex("#D8B059");

    /// <summary>
    /// The range in which players can't get past.
    /// </summary>
    [DataField]
    public float RestrictedRange = 512f;

    /// <summary>
    /// Effects to run on the map.
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects;

    /// <summary>
    /// The gas mixture to use for the atmosphere.
    /// </summary>
    [DataField(required: true)]
    public GasMixture Atmosphere = new();

    /// <summary>
    /// If the map has ruins to spawn.
    /// </summary>
    [DataField]
    public ProtoId<LavalandRuinPoolPrototype>? RuinPool;

    /// <summary>
    /// Biome layers to add to the map, i.e. ores.
    /// </summary>
    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> BiomeMarkerLayers = new();
}
