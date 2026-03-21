// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Kudzu;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeeOverObstacleComponent : Component
{
    // Keep raw depth values here so Trauma.Common stays decoupled from Content.Shared.
    [DataField, AutoNetworkedField]
    public int NormalDrawDepth = 10;

    [DataField, AutoNetworkedField]
    public int SeeOverDrawDepth = -5;
}
