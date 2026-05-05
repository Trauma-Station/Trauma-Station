// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.EntitySystems;
using Content.Server.Examine;
using Content.Shared.Atmos.Components;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class IgniteOnFlashSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    private readonly HashSet<Entity<FlammableComponent>> _targetEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnFlashComponent, AfterFlashActivatedEvent>(OnFlashActivated);
    }

    private void OnFlashActivated(Entity<IgniteOnFlashComponent> ent, ref AfterFlashActivatedEvent args)
    {
        if (args.Target != null || !TryComp(ent, out FlashComponent? flash))
            return;

        var coords = _transform.GetMapCoordinates(ent);

        _targetEntities.Clear();
        _look.GetEntitiesInRange(coords, flash.Range, _targetEntities, LookupFlags.Dynamic);
        foreach (var (uid, flam) in _targetEntities)
        {
            if (uid == args.User)
                continue;

            if (!_examine.InRangeUnOccluded(uid, coords, flash.Range))
                continue;

            _flammable.AdjustFireStacks(uid,
                ent.Comp.FireStacks,
                flam,
                penetration: ent.Comp.FireProtectionPenetration);
            if (ent.Comp.FireStacks > 0f)
                _flammable.Ignite(uid, ent, flam, args.User);
        }
    }
}
