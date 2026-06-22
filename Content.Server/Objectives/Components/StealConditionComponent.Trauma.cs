namespace Content.Server.Objectives.Components;

public sealed partial class StealConditionComponent
{
    /// <summary>
    /// Does the objective instead require just a scan of the theft item instead of holding it?
    /// </summary>
    [DataField]
    public bool RequireScan = false;
}
