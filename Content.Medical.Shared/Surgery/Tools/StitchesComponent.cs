using Content.Medical.Common.Surgery.Tools;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Tools;

/// <summary>
///     It lets you fucking stitch your ass up
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StitchesComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "stitches";
    [DataField]
    public bool? Used { get; set; } = null;
    [DataField]
    public float Speed { get; set; } = 1f;
}
