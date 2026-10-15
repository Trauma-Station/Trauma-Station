// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.NPC.Prototypes;

namespace Content.Trauma.Shared.Illusion;

/// <summary>
/// Added to entities that can spawn illusions in some way or another
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IllusionSpawnerComponent : Component
{
    /// <summary>
    /// Chance to spawn an illusion
    /// </summary>
    [DataField]
    public float Chance = 0.5f;

    /// <summary>
    /// Illusion time to despawn
    /// </summary>
    [DataField]
    public float Lifetime = 10f;

    /// <summary>
    /// Illusion hp multiplier
    /// </summary>
    [DataField]
    public float HealthMultiplier = 0.4f;

    /// <summary>
    /// If this item is copied over, <see cref="Chance/>" and <see cref="HealthMultiplier"/> will be multiplied by this
    /// </summary>
    [DataField]
    public float DegradationRateOnClone = 0.5f;

    /// <summary>
    /// If true, this will not spawn illusions when triggered by mobs with whitelisted factions
    /// </summary>
    [DataField]
    public bool IgnoreSameFactionTargets;

    /// <summary>
    /// Only spawn illusions if user belongs to these factions
    /// </summary>
    [DataField]
    public List<ProtoId<NpcFactionPrototype>> FactionWhitelist = new();

    /// <summary>
    /// Components to add to illusions
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
