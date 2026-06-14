// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Particles;

namespace Content.Trauma.Client.Particles;

public sealed partial class SpawnParticlesEffectSystem : SharedSpawnParticlesEffectSystem
{
    [Dependency] private ParticleSystem _particles = default!;

    protected override void SpawnParticles(ProtoId<ParticleEffectPrototype> particleProto, EntityUid target, Color? color, bool attached)
    {
        base.SpawnParticles(particleProto, target, color, attached);

        _particles.CreateParticle(particleProto, target, color, attached);
    }
}
