// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Animations;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Animations;

public sealed class FlipOnHitSystem : SharedFlipOnHitSystem
{
    protected override void PlayAnimation(EntityUid user)
    {
        RaiseNetworkEvent(new FlipOnHitEvent(GetNetEntity(user)),
            Filter.PvsExcept(user, entityManager: EntityManager));
    }

    protected override void StopAnimation(EntityUid user)
    {
        RaiseNetworkEvent(new FlipOnHitStopEvent(GetNetEntity(user)),
            Filter.PvsExcept(user, entityManager: EntityManager));
    }
}
