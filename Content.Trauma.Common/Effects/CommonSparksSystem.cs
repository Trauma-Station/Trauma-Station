// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Common.Effects;

public abstract class CommonSparksSystem : EntitySystem
{
    public const int MinSparks = 1;
    public const int MaxSparks = 3;
    public const float MinVelocity = 0.5f;
    public const float MaxVelocity = 2f;

    public abstract void DoSparks(EntityCoordinates coords,
        EntityUid? user = null,
        int minSparks = MinSparks,
        int maxSparks = MaxSparks,
        float minVelocity = MinVelocity,
        float maxVelocity = MaxVelocity,
        bool playSound = true,
        EntityUid? source = null);

    public void DoSparks(EntityUid uid,
        EntityUid? user = null,
        int minSparks = MinSparks,
        int maxSparks = MaxSparks,
        float minVelocity = MinVelocity,
        float maxVelocity = MaxVelocity,
        bool playSound = true,
        EntityUid? source = null)
    {
        DoSparks(Transform(uid).Coordinates, user, minSparks, maxSparks, minVelocity, maxVelocity, playSound, source: source ?? uid);
    }
}
