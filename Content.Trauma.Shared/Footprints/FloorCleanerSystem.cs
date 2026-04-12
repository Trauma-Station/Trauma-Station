// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Decals;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Footprints;

public sealed class FloorCleanerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloorCleanerComponent, AfterInteractEvent>(OnAfterInteract,
            before: [ typeof(SharedAbsorbentSystem) ]);
    }

    private void OnAfterInteract(Entity<FloorCleanerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.CanReach && !args.Handled && CleanDecals(ent, args.ClickLocation))
            args.Handled = true;
    }

    public bool CleanDecals(Entity<FloorCleanerComponent> ent, EntityCoordinates coords)
    {
        if (Transform(ent).GridUid is not {} grid ||
            TryComp<UseDelayComponent>(ent, out var delay) && _delay.IsDelayed((ent, delay)))
            return false;

        var pos = _transform.WithEntityId(coords, grid).Position;
        var decals = _decal.GetDecalsInRange(grid, pos, ent.Comp.Radius);
        var cleaned = false;

        // actually clean them. not predicted since decal shitcode is serverside
        foreach (var decal in decals)
        {
            if (!decal.Decal.Cleanable)
                continue;
            _decal.RemoveDecal(grid, decal.Index);
            cleaned = true;
        }

        if (!cleaned)
            return false;

        // TODO: change to PlayPredicted if decals ever gets moved to shared (lol never happening)
        _audio.PlayPvs(ent.Comp.Sound, ent);

        if (delay != null)
            _delay.TryResetDelay((ent, delay));
        return true;
    }
}
