// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Body;
using Content.Trauma.Shared.Wizard.Traps;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Ice bricks the ent.
/// </summary>
public sealed partial class IceBrick : EntityEffectBase<IceBrick>
{
    [DataField(required: true)]
    public TimeSpan BreakFreeDelay;
}

public sealed partial class IceBrickSystem : EntityEffectSystem<BodyComponent, IceBrick>
{
    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<IceBrick> args)
    {
        var IceCubeComp = EnsureComp<IceCubeComponent>(ent.Owner);
        IceCubeComp.BreakFreeDelay = args.Effect.BreakFreeDelay;
        Dirty(ent.Owner, IceCubeComp);
    }
}
