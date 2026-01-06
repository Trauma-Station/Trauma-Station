namespace Content.Shared.Construction;

/// <summary>
/// Trauma - virtual methods for calling from shared code
/// </summary>
public abstract partial class SharedConstructionSystem
{
    public virtual bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => false;
}
