// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

public abstract class CommonGhostVisibilitySystem : EntitySystem
{
    /// <summary>
    ///     Determines whether ghosts are currently visible to living players.
    /// </summary>
    public abstract bool GhostsVisible();

    /// <summary>
    /// Determines whether a ghost can be seen by other ghosts.
    /// </summary>
    public abstract bool IsVisible(EntityUid uid);
}
