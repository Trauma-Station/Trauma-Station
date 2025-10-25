namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Objective that requires a salvager to claim a points quota configured by cvars.
/// </summary>
[RegisterComponent]
public sealed partial class ClaimPointsConditionComponent : Component
{
    /// <summary>
    /// Loc string that has "quota" passed to it.
    /// </summary>
    [DataField(required: true)]
    public LocId Desc;
}
