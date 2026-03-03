using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TacklingComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetCoordinates TackleStartPosition;

    [DataField, AutoNetworkedField]
    public EntityUid Source;
}
