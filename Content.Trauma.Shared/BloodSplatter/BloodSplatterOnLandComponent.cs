// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.BloodSplatter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodSplatterOnLandComponent : Component
{
    [DataField]
    public EntProtoId Decal = "DecalSpawnerBloodSplattersTrauma";

    [DataField, AutoNetworkedField]
    public Color Color = Color.Red;

    [DataField]
    public bool DeleteEntity = true;
}
