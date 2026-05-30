namespace Content.Trauma.Server.Vampires.Objectives;

/// <summary>
/// Objective component that checks how many victims we have sucked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VampireSuckConditionComponent : Component
{
    /// <summary>
    /// The entities we have sucked.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> SuckedEntities = new();
}
