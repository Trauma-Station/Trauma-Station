// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Effects that adjusts the total and usable blood of a vampire.
/// </summary>
public sealed partial class AdjustVampireBlood : EntityEffectBase<AdjustVampireBlood>
{
    /// <summary>
    /// By how much to adjust the vampire's total and usable blood.
    /// </summary>
    [DataField(required: true)]
    public int Amount;
}

public sealed partial class AdjustVampireBloodEffectSystem : EntityEffectSystem<VampireComponent, AdjustVampireBlood>
{
    [Dependency] private VampireSystem _vampire = default!;

    protected override void Effect(Entity<VampireComponent> entity, ref EntityEffectEvent<AdjustVampireBlood> args)
    {
        var scale = (int) args.Scale;
        _vampire.AdjustBlood(entity.AsNullable(), args.Effect.Amount * scale);
    }
}
