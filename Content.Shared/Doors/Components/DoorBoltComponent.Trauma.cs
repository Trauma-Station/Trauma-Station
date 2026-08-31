using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Shared.Doors.Components;

public sealed partial class DoorBoltComponent
{
    /// <summary>
    /// Tool that can be used to raise the bolts when unpowered.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> UnboltToolQuality = "Anchoring";

    [DataField]
    public TimeSpan ManualUnboltTime = TimeSpan.FromSeconds(10);
}
