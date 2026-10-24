// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Component for Aer-124, lets them inject all people aroud her with sad chems
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AerSlimeSadComponent : Component
{
    [DataField, AutoNetworkedField]
    public required Dictionary<string, FixedPoint2> Reagents;

    [DataField]
    public float Range = 10f;
};
