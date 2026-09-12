// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// Scan anomalous entities for linking them to containment chambers
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnomalousEntityScannerComponent : Component
{
    /// <summary>
    /// The anomalous entity that was last scanned by this scanner.
    /// </summary>
    [DataField]
    public EntityUid? ScannedAER;

    /// <summary>
    /// How long the scan takes
    /// </summary>
    [DataField]
    public float ScanDoAfterDuration = 5;

    /// <summary>
    /// The sound plays when the scan finished
    /// </summary>
    [DataField]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");
}
