// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Body;
using Content.Trauma.Shared.Wizard.Traps;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Ice bricks all entitys in set range.
/// </summary>
public sealed partial class IceBrickInRange : EntityEffectBase<IceBrickInRange>
{
    [DataField]
    public float SeachRange = 1.0f;

    [DataField]
    public bool IgnoreSelf = true;

    [DataField(required: true)]
    public TimeSpan BreakFreeDelay;
}

public sealed partial class IceBrickInRangeSystem : EntityEffectSystem<BodyComponent, IceBrickInRange>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<IceBrickInRange> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<BodyComponent>(entPos, args.Effect.SeachRange)){
            if (args.Effect.IgnoreSelf && uid == ent.Owner)
                continue;
            var IceCubeComp = EnsureComp<IceCubeComponent>(uid);
            IceCubeComp.BreakFreeDelay = args.Effect.BreakFreeDelay;
            Dirty(uid, IceCubeComp);
        }
    }
}
