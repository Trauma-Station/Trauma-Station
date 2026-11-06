// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Procedural.Prototypes;

/// <summary>
/// Contains information about Lavaland ruin configuration.
/// </summary>
[Prototype]
public sealed partial class LavalandGridRuinPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this ruin
    /// </summary>
    [DataField]
    public LocId Name = "lavaland-ruin-unknown";

    /// <summary>
    /// The path to the ruin's map file
    /// </summary>
    [DataField(required: true)]
    public ResPath Path;

    /// <summary>
    /// How many attempts to run while trying to spawn this ruin
    /// </summary>
    [DataField]
    public int SpawnAttempts = 8;

    /// <summary>
    /// Whether to patch it to the planet
    /// </summary>
    [DataField]
    public bool PatchToPlanet = true;

    /// <summary>
    /// The priority of the ruin
    /// </summary>
    [DataField(required: true)]
    public int Priority = int.MinValue;

    /// <summary>
    /// List of components to grant to entities that enter the ruin.
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsToGrant = new();
}
