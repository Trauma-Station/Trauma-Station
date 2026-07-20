// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Blob.Chemistry;

/// <summary>
/// This is used for... rider major
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BlobSmokeColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color;
}
