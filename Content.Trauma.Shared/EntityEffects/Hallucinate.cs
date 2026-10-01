// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Body;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Robust.Shared.GameObjects;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Ice bricks all entitys in set range.
/// </summary>
public sealed partial class Hallucinate : EntityEffectBase<Hallucinate>
{
    [DataField]
    public float SeachRange = 1.0f;

    [DataField]
    public bool IgnoreSelf = true;

    [DataField]
    public float FearAmount = 50.0f; // Is it even doing anything besides "scary" visual effect?
}

public sealed partial class FearEffectSystem : EntityEffectSystem<BodyComponent, Hallucinate>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedFearSystem _fear = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<Hallucinate> args)
    {
        var entPos = _transform.GetMapCoordinates(ent.Owner);
        foreach (var (uid, comp) in _lookup.GetEntitiesInRange<BodyComponent>(entPos, args.Effect.SeachRange)){
            if (args.Effect.IgnoreSelf && uid == ent.Owner)
                continue;
            _fear.AdjustFear(uid, uid, args.Effect.FearAmount);

            // is there a better way to do it then just writing same shit 30 times?... RelayNearby...
        }
    }
}
