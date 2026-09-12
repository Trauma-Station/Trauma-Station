namespace Content.Server.Objectives.Components;

public sealed partial class TargetObjectiveComponent
{
    /// <summary>
    /// Whether name for this objective would change when person's mind attaches to other entity.
    /// </summary>
    [DataField]
    public bool DynamicName;

    /// <summary>
    /// Whether job name should be shown in objective name
    /// </summary>
    [DataField]
    public bool ShowJobTitle = true;
}
