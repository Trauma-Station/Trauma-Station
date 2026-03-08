namespace Content.Server.Construction;

/// <summary>
/// Trauma - helper for shared code to call server methods
/// </summary>
public sealed partial class ConstructionSystem
{
    public override bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => ChangeNode(uid, userUid, id, performActions, null);
}
