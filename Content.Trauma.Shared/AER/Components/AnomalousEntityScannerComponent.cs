using Robust.Shared.Audio;
using Robust.Shared.GameStates;

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
    [ViewVariables]
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