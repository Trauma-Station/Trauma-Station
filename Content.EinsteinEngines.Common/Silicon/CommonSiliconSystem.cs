namespace Content.EinsteinEngines.Common.Silicon;

public abstract class CommonSiliconSystem : EntitySystem
{
    /// <summary>
    /// API that checks if an entity is a silicon or not.
    /// </summary>
    public abstract bool IsSilicon(EntityUid uid);
}
