// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Body;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Ice bricks all entitys in set range.
/// </summary>
public sealed partial class Fear : EntityEffectBase<Fear>
{
    [DataField]
    public float SeachRange = 1.0f;

    [DataField]
    public bool IgnoreSelf = true;

    [DataField]
    public float FearAmount = 2.0f;
}

public sealed partial class FearEffectSystem : EntityEffectSystem<BodyComponent, Fear>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedFearSystem _fear = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<Fear> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<BodyComponent>(entPos, args.Effect.SeachRange)){
            if (args.Effect.IgnoreSelf && uid == ent.Owner)
                continue;
            _fear.AddFear(uid, args.Effect.FearAmount, uid);
        }
    }
}
