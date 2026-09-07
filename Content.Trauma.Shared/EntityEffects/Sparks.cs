// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Effects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Play spark effects at the target entity.
/// </summary>
[DataRecord]
public sealed partial class Sparks : EntityEffectBase<Sparks>
{
    public int MinSparks = SparksSystem.MinSparks;

    public int MaxSparks = SparksSystem.MaxSparks;

    public float MinVelocity = SparksSystem.MinVelocity;

    public float MaxVelocity = SparksSystem.MaxVelocity;

    public bool PlaySound = true;
}

public sealed partial class SparksEffectSystem : EntityEffectSystem<TransformComponent, Sparks>
{
    [Dependency] private SparksSystem _sparks = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Sparks> args)
    {
        var e = args.Effect;
        _sparks.DoSparks(ent.Comp.Coordinates, args.User, e.MinSparks, e.MaxSparks,
            e.MinVelocity, e.MaxVelocity, e.PlaySound, source: ent);
    }
}
