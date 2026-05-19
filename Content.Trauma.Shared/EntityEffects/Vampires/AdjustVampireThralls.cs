// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Vampires.Dantalion;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Effect that adjusts the cap on an entity with <see cref="VampireThrallsComponent"/>.
/// </summary>
public sealed partial class AdjustVampireThralls : EntityEffectBase<AdjustVampireThralls>
{
    /// <summary>
    /// By how much to increase the cap.
    /// </summary>
    [DataField]
    public int Amount = 1;
}

public sealed partial class AdjustVampireThrallsEffectSystem : EntityEffectSystem<VampireThrallsComponent, AdjustVampireThralls>
{
    [Dependency] private VampireThrallSystem _thrall = default!;

    protected override void Effect(Entity<VampireThrallsComponent> ent, ref EntityEffectEvent<AdjustVampireThralls> args)
    {
        var effect = args.Effect;

        _thrall.AdjustThrallCap(ent.AsNullable(), effect.Amount);
    }
}
