// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Effects;
using Robust.Client.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Client.Effects;

public sealed partial class SparksEffectSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId SparkPrototype = "EffectSpark";

    private static readonly SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    [EventSubscription] // W butchering the name 4 no raisin
    private void OnSparksEffect(SparksEffectEvent args)
    {
        var coords = GetCoordinates(args.Coords);
        if (!coords.IsValid(EntityManager))
            return;

        if (args.PlaySound)
            _audio.PlayPvs(Sound, coords);

        var mapCoords = _transform.ToMapCoordinates(coords);
        for (var i = 0; i < args.Amount; i++)
        {
            var velocity = _random.NextFloat(args.MinVel, args.MaxVel);
            var dir = _random.NextAngle().ToVec() * velocity;
            var spark = Spawn(SparkPrototype, mapCoords);
            _physics.SetLinearVelocity(spark, dir);
        }
    }
}
