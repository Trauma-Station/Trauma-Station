// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Vampires.Dantalion;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Effect that adjustes the cap on an entity with <see cref="VampireThrallsComponent"/>.
/// </summary>
public sealed partial class AdjustVampireThralls : EntityEffectBase<AdjustVampireThralls>
{
    /// <summary>
    /// By how much to increase the cap.
    /// </summary>
    [DataField]
    public int Amount = 1;
}

public sealed class AdjustVampireThrallsEffectSystem : EntityEffectSystem<VampireThrallsComponent, AdjustVampireThralls>
{
    protected override void Effect(Entity<VampireThrallsComponent> ent, ref EntityEffectEvent<AdjustVampireThralls> args)
    {
        var effect = args.Effect;

        ent.Comp.ThrallCap += effect.Amount;
        Dirty(ent);
    }
}
