// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that applies nullification on the target.
/// </summary>
public sealed partial class ApplyNullification : EntityEffectBase<ApplyNullification>
{
    /// <summary>
    /// How much nullification to add.
    /// </summary>
    [DataField(required: true)]
    public int Amount;
}

public sealed partial class ApplyNullificationEffectSystem : EntityEffectSystem<NullificationComponent, ApplyNullification>
{
    [Dependency] private NullificationSystem _nullification = default!;

    protected override void Effect(Entity<NullificationComponent> ent, ref EntityEffectEvent<ApplyNullification> args)
    {
        var effect = args.Effect;
        _nullification.AdjustNullification(ent.AsNullable(), effect.Amount * (int)MathF.Round(args.Scale));
    }
}
