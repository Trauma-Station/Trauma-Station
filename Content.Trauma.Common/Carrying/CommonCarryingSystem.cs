namespace Content.Trauma.Common.Carrying;

public abstract class CommonCarryingSystem : EntitySystem
{
    public abstract void DropCarried(EntityUid carrier, EntityUid carried);
}
