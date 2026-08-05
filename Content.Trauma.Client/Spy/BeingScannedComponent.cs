// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Spy;

/// <summary>
/// Used for visual effect when entity is being scanned for spy bounty
/// </summary>
[RegisterComponent]
public sealed partial class BeingScannedComponent : Component
{
    public EntityUid Scanner;

    public float Ratio;
}
