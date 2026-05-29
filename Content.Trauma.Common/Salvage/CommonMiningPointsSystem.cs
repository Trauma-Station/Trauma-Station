// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Salvage;

public abstract partial class CommonMiningPointsSystem : EntitySystem
{
    /// <summary>
    /// Returns true if the user has at least some number of points on their ID card.
    /// </summary>
    public abstract bool UserHasPoints(EntityUid user, uint points);


    /// <summary>
    /// if user can claim mining points
    /// <summary>
    public abstract bool CanClaimPoints(EntityUid user);

    /// <summary>
    /// Tries to find the user's id card and gets its <see cref="MiningPointsComponent"/>.
    /// </summary>
    /// <remarks>
    /// Component is nullable for easy usage with the API due to Entity&lt;T&gt; not being usable for Entity&lt;T?&gt; arguments.
    /// </remarks>
    public abstract Entity<MiningPointsComponent?>? TryFindIdCard(EntityUid user);

    /// <summary>
    /// Removes points from a holder, returning true if it succeeded.
    /// </summary>
    public abstract bool RemovePoints(Entity<MiningPointsComponent?> ent, uint amount);
}
