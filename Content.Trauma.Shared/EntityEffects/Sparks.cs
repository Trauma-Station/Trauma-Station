// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Effects;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Play spark effects at the target entity.
/// </summary>
[DataRecord]
public sealed partial class Sparks : EntityEffectBase<Sparks>
{
    public int MinSparks = 1;

    public int MaxSparks = 3;

    public float MinVelocity = 1f;

    public float MaxVelocity = 4f;

    public bool PlaySound = true;
}

public sealed partial class SparksEffectSystem : EntityEffectSystem<TransformComponent, Sparks>
{
    [Dependency] private CommonSparksSystem _sparks = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Sparks> args)
    {
        var e = args.Effect;
        _sparks.DoSparks(e.MinSparks, e.MaxSparks, e.MinVelocity, e.MaxVelocity, e.PlaySound, args.Predicted, ent);
    }
}
