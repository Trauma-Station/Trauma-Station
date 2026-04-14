// SPDX-License-Identifier: AGPL-3.0-or-later

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
