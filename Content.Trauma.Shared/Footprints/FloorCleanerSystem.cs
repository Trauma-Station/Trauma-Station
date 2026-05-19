// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Decals;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Footprints;

public sealed partial class FloorCleanerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDecalSystem _decal = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloorCleanerComponent, AfterInteractEvent>(OnAfterInteract,
            before: [ typeof(SharedAbsorbentSystem) ]);
    }

    private void OnAfterInteract(Entity<FloorCleanerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.CanReach && !args.Handled && CleanDecals(ent, args.ClickLocation, args.User))
            args.Handled = true;
    }

    public bool CleanDecals(Entity<FloorCleanerComponent> ent, EntityCoordinates coords, EntityUid? user)
    {
        if (Transform(ent).GridUid is not {} grid ||
            TryComp<UseDelayComponent>(ent, out var delay) && _delay.IsDelayed((ent, delay)))
            return false;

        var pos = _transform.WithEntityId(coords, grid).Position;
        var decals = _decal.GetDecalsInRange(grid, pos, ent.Comp.Radius);
        var cleaned = false;

        foreach (var decal in decals)
        {
            if (!decal.Comp.Data.Cleanable)
                continue;

            PredictedDel(decal.Owner);
            cleaned = true;
        }

        if (!cleaned)
            return false;

        _audio.PlayPredicted(ent.Comp.Sound, ent, user);

        if (delay != null)
            _delay.TryResetDelay((ent, delay));
        return true;
    }
}
