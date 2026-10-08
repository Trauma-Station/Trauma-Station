// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Effects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Effects;

public sealed partial class SparksSystem : CommonSparksSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void DoSparks(EntityCoordinates coords,
        EntityUid? user = null,
        int minSparks = MinSparks,
        int maxSparks = MaxSparks,
        float minVelocity = MinVelocity,
        float maxVelocity = MaxVelocity,
        bool playSound = true,
        EntityUid? source = null)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(coords.EntityId), GetNetEntity(source ?? user));
        var amount = rand.Next(minSparks, maxSparks + 1);

        if (amount <= 0)
            return;

        if (minVelocity > maxVelocity)
            maxVelocity = minVelocity;

        var ev = new SparksEffectEvent(GetNetCoordinates(coords), amount, minVelocity, maxVelocity, playSound);
        if (_net.IsServer)
            RaiseNetworkEvent(ev, Filter.Pvs(coords).RemoveWhereAttachedEntity(e => e == user));
        else if (_timing.IsFirstTimePredicted)
            RaiseLocalEvent(ev);
    }
}

/// <summary>
/// Event that does the actual sound/entity in <c>SparksEffectSystem</c>.
/// </summary>
[Serializable, NetSerializable]
public sealed class SparksEffectEvent(NetCoordinates coords, int amount, float minVel, float maxVel, bool playSound) : EntityEventArgs
{
    public NetCoordinates Coords = coords;
    public int Amount = amount;
    public float MinVel = minVel;
    public float MaxVel = maxVel;
    public bool PlaySound = playSound;
}
