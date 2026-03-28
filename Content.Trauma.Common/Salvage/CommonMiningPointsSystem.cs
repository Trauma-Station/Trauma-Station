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
}
