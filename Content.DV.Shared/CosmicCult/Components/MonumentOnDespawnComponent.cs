using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Spawns a specified prototype after a specified amount of time. Too lazy to rename the comp, sorry.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class MonumentOnDespawnComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? SpawnTimer;

    [DataField]
    public TimeSpan SpawnTime = TimeSpan.FromSeconds(1);
}
