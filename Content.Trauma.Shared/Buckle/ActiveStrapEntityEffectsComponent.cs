using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Added to <see cref="StrapEntityEffectsComponent"/> when something is strapped to it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapEntityEffectsSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveStrapEntityEffectsComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
