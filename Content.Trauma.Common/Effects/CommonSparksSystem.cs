// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Common.Effects;

public abstract class CommonSparksSystem : EntitySystem
{
    public abstract void DoSparks(EntityCoordinates coords,
        EntityUid? user = null,
        int minSparks = 1,
        int maxSparks = 3,
        float minVelocity = 1f,
        float maxVelocity = 4f,
        bool playSound = true,
        EntityUid? source = null);

    public void DoSparks(EntityUid uid,
        EntityUid? user = null,
        int minSparks = 1,
        int maxSparks = 3,
        float minVelocity = 1f,
        float maxVelocity = 4f,
        bool playSound = true,
        EntityUid? source = null)
    {
        DoSparks(Transform(uid).Coordinates, user, minSparks, maxSparks, minVelocity, maxVelocity, playSound, source: source ?? uid);
    }
}
