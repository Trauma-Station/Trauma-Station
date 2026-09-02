// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Particles;

namespace Content.Trauma.Client.Particles;

public sealed partial class SpawnParticlesEffectSystem : SharedSpawnParticlesEffectSystem
{
    [Dependency] private ParticleSystem _particles = default!;

    [SubscribeNetworkEvent]
    private void OnSpawnParticles(SpawnParticlesEvent args)
    {
        SpawnParticles(args.ParticleProto, GetEntity(args.Target), args.Color, args.Attached, args.Number, null);
    }

    protected override void SpawnParticles(ProtoId<ParticleEffectPrototype> particleProto,
        EntityUid target,
        Color? color,
        bool attached,
        int number,
        EntityUid? user)
    {
        base.SpawnParticles(particleProto, target, color, attached, number, user);

        for (var i = 0; i < number; i++)
        {
            _particles.CreateParticle(particleProto, target, color, attached);
        }
    }
}
