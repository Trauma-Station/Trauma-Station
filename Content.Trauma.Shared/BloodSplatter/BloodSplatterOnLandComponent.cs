// SPDX-License-Identifier: AGPL-3.0-or-later


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
