// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Teleportation.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Teleportation.Systems;

/// <summary>
/// This handles pocket dimensions and their portals.
/// </summary>
public sealed partial class PocketDimensionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PocketDimensionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.PocketDimensionMap is { } map && Exists(map))
            PredictedQueueDel(map);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<PocketDimensionComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("pocket-dimension-verb-text"),
            Act = () => HandleActivation(ent, user)
        });
    }

    /// <summary>
    /// Handles toggling the portal to the pocket dimension.
    /// </summary>
    private void HandleActivation(Entity<PocketDimensionComponent> ent, EntityUid user)
    {
        var (uid, comp) = ent;
        if (Deleted(comp.PocketDimensionMap))
        {
            if (!_timing.IsFirstTimePredicted)
                return; // dont want to try loading a map 10 times lol

            if (!_mapLoader.TryLoadMap(comp.PocketDimensionPath, out var map, out var roots,
                options: new Robust.Shared.EntitySerialization.DeserializationOptions { InitializeMaps = true }))
            {
                Log.Error($"Failed to load pocket dimension map {comp.PocketDimensionPath}");
                QueueDel(map);
                return;
            }

            comp.PocketDimensionMap = map;

            // find the pocket dimension's first grid and put the portal there
            bool foundGrid = false;
            foreach (var root in roots)
            {
                if (!HasComp<MapGridComponent>(root))
                    continue;

                comp.RootGrid = root;

                // spawn the permanent portal into the pocket dimension, now ready to be used
                SpawnExitPortal(ent, root);
                // the TryUnlink cleanup when first trying to create portal will fail without this
                EnsureComp<LinkedEntityComponent>(ent);

                Log.Info($"Created pocket dimension on grid {root} of map {map}");

                // if someone closes your portal you can use the one inside to escape
                _link.OneWayLink(comp.ExitPortal!.Value, uid);
                foundGrid = true;
                break;
            }
            if (!foundGrid)
            {
                Log.Error($"Pocket dimension {comp.PocketDimensionPath} had no grids!");
                Del(map);
                comp.PocketDimensionMap = null;
                return;
            }
            Dirty(ent);
        }

        // respawn exit portal if something deleted it
        if (Deleted(comp.ExitPortal))
            SpawnExitPortal(ent, comp.RootGrid!.Value);

        var dimension = comp.ExitPortal!.Value;
        if (comp.PortalEnabled)
        {
            // unlink us
            _link.TryUnlink(dimension, uid);
            comp.PortalEnabled = false;
            _audio.PlayPvs(comp.ClosePortalSound, uid);

            // if you are stuck inside the pocket dimension you can use the internal portal to escape
            _link.OneWayLink(dimension, uid);
        }
        else
        {
            // cleanup
            _link.TryUnlink(dimension, uid);
            // link us to the pocket dimension
            _link.TryLink(dimension, uid);
            comp.PortalEnabled = true;
            _audio.PlayPvs(comp.OpenPortalSound, uid);
        }
    }

    private void SpawnExitPortal(Entity<PocketDimensionComponent> ent, EntityUid grid)
    {
        var pos = new EntityCoordinates(grid, 0, 0);
        var portal = PredictedSpawnAtPosition(ent.Comp.ExitPortalPrototype, pos);
        ent.Comp.ExitPortal = portal;
        Dirty(ent);
    }
}
