// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlobResourceComponent : Component
{
    [DataField]
    public int PointsPerPulsed = 3;
}
