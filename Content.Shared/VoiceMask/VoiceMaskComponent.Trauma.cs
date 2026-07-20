using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.VoiceMask;

public sealed partial class VoiceMaskComponent
{
    /// <summary>
    /// Whether the action should be granted when equipped.
    /// </summary>
    [DataField]
    public bool EnableAction = true;

    /// <summary>
    /// The job icon to be displayed next to their name when speaking on radio
    /// </summary>
    [DataField]
    public ProtoId<JobIconPrototype>? JobIconProtoId;

    /// <summary>
    /// The name of the job that should show up when a mouse overs over the job icon on the radio
    /// </summary>
    [DataField]
    public string? JobName;
}
