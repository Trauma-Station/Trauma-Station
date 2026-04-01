namespace Content.Trauma.Common.Xenomorphs;

public abstract partial class CommonXenomorphSystem : EntitySystem
{
    /// <summary>
    /// Determines if a user is currently blocked from their ghost returning to their body.
    /// </summary>
    public abstract bool IsSlimed(EntityUid uid);
}
