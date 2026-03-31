namespace Content.Trauma.Common.Wizard;

public abstract class CommonGhostVisibilitySystem : EntitySystem
{
    /// <summary>
    ///     Determines whether ghosts are currently visible to living players.
    /// </summary>
    public abstract bool GhostsVisible();
}
