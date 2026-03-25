namespace Content.Shitcode.Common.Wizard;

public abstract partial class CommonActionTargetMarkSystem : EntitySystem
{
    public abstract void SetMark(Entity<LockOnMarkActionComponent> ent, EntityUid? targetUid);
}
