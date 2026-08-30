// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Particles;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Particles;

public sealed class SpawnParticlesEffectSystem : SharedSpawnParticlesEffectSystem
{
    protected override void SpawnParticles(ProtoId<ParticleEffectPrototype> particleProto,
        EntityUid target,
        Color? color,
        bool attached,
        int number,
        EntityUid? user)
    {
        base.SpawnParticles(particleProto, target, color, attached, number, user);

        var filter = Filter.Pvs(target);
        if (user is { } u)
            filter = filter.RemoveWhereAttachedEntity(e => e == u);
        var ev = new SpawnParticlesEvent(GetNetEntity(target), particleProto, attached, number, color);
        RaiseNetworkEvent(ev, filter);
    }
}
