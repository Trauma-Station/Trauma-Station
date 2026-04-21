// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Client.Particles;
using Content.Trauma.Shared.Particles;

namespace Content.Trauma.Client.EntityEffects;

/// <summary>
/// Spawns particles at the current position of the entity.
/// </summary>
public sealed partial class SpawnParticles : EntityEffectBase<SpawnParticles>
{
    /// <summary>
    /// The particles to spawn
    /// </summary>
    [DataField]
    public ProtoId<ParticleEffectPrototype> ParticleProto;

    /// <summary>
    /// If true, it will attach to the entity
    /// </summary>
    [DataField]
    public bool Attached;

    /// <summary>
    /// Amount of particles we're spawning
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// If set, it will override the colour of the particle
    /// </summary>
    [DataField]
    public Color? Color;
}

public sealed class SpawnParticlesEffectSystem : EntityEffectSystem<TransformComponent, SpawnParticles>
{
    [Dependency] private readonly ParticleSystem _particles = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnParticles> args)
    {
        var effect = args.Effect.ParticleProto;
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var color = args.Effect.Color;
        var attach = args.Effect.Attached;

        for (int i = 0; i < quantity; i++)
        {
            _particles.CreateParticle(effect, ent.Owner, color, attach);
        }
    }
}
