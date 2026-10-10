// SPDX-License-Identifier: AGPL-3.0-or-later

/// <summary>
/// The base type for the <see cref="JobListingsSystem"/>.
/// </summary>
public abstract partial class CommonJobListingsSystem : EntitySystem
{
    /// <summary>
    /// Is the job board this entity points to hidden?
    /// </summary>
    public abstract bool IsRemoteJobBoardHidden(EntityUid remote);
}
