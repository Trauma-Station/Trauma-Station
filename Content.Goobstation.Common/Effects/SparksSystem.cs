// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Goobstation.Common.Effects;

public abstract class CommonSparksSystem : EntitySystem
{
    public abstract void DoSparks(EntityCoordinates coords,
        int minSparks = 1,
        int maxSparks = 3,
        float minVelocity = 1f,
        float maxVelocity = 4f,
        bool playSound = true,
        bool predicted = true,
        EntityUid? source = null);

    public void DoSparks(EntityUid uid,
        int minSparks = 1,
        int maxSparks = 3,
        float minVelocity = 1f,
        float maxVelocity = 4f,
        bool playSound = true,
        bool predicted = true)
    {
        DoSparks(Transform(uid).Coordinates, minSparks, maxSparks, minVelocity, maxVelocity, playSound, predicted, source: uid);
    }
}
