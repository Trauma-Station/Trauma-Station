using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Shitcode.Heretic.Components;

/// <summary>
/// Tracks environment temperature serverside and sends it to client
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TemperatureTrackerComponent : Component
{
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Environment temperature
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature;
}
