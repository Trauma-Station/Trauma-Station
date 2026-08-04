// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Spy;

[RegisterComponent]
public sealed partial class BeingScannedComponent : Component
{
    [DataField]
    public EntityUid Scanner;

    [DataField]
    public float Ratio;
}
