using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Lollypop;

/// <summary>
/// Component added to lollypops which are in the mask slot (being slowly eaten).
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LollypopSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EquippedLollypopComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? HeldBy;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? NextBite;

    /// <summary>
    /// Max solution of the lollypop to eat for each update.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxEaten = 1;
}
