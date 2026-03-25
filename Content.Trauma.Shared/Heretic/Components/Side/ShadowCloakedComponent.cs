// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowCloakedComponent : Component
{
    [ViewVariables]
    public bool WasVisible = true;

    [DataField]
    public EntProtoId ShadowCloakEntity = "ShadowCloakEntity";
}
