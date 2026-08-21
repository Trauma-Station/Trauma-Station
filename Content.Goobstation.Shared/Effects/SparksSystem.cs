// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Effects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Effects;

public sealed partial class SparksSystem : CommonSparksSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly EntProtoId SparkPrototype = "EffectSpark";

    private static readonly SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    public override void DoSparks(EntityCoordinates coords,
        int minSparks = 1,
        int maxSparks = 3,
        float minVelocity = 1f,
        float maxVelocity = 4f,
        bool playSound = true,
        bool predicted = true,
        EntityUid? source = null)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(coords.EntityId), GetNetEntity(source));
        var amount = rand.Next(minSparks, maxSparks + 1);

        if (amount <= 0)
            return;

        if (playSound)
        {
            if (predicted ? (_net.IsClient && _timing.IsFirstTimePredicted) : _net.IsServer)
                _audio.PlayPvs(Sound, coords);
        }

        var mapCoords = _transform.ToMapCoordinates(coords);

        float? velocityOverride = minVelocity < maxVelocity ? null : minVelocity;

        for (var i = 0; i < amount; i++)
        {
            var velocity = velocityOverride ?? rand.NextFloat(minVelocity, maxVelocity);
            var dir = rand.NextAngle().ToVec() * velocity;
            var spark = EntityManager.PredictedSpawn(SparkPrototype, mapCoords);
            _physics.SetLinearVelocity(spark, dir);
        }
    }
}
