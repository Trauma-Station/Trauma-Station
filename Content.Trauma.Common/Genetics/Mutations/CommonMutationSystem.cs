// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Genetics.Mutations;

/// <summary>
/// Minimal API for other modules to use.
/// </summary>
public abstract class CommonMutationSystem : EntitySystem
{
    /// <summary>
    /// Collects mutatable data from a mob.
    /// Returns empty lists if it isn't mutatable.
    /// </summary>
    public abstract MutatableData GetMutatableData(EntityUid mob);

    /// <summary>
    /// Clears a mob's mutations then loads new ones from a data struct.
    /// Returns true if the data was loaded.
    /// </summary>
    public abstract bool LoadMutatableData(EntityUid mob, MutatableData data);
}
