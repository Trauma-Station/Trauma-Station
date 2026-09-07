// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Chasm;
using Content.Shared.Chasm;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Timing;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Lavaland.Shared.Chasm;

public sealed partial class PreventChasmFallingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, BeforeChasmFallingEvent>(Relay);
    }

    private HashSet<Entity<ChasmComponent>> _chasms = new();

    [SubscribeLocalEvent]
    private void OnBeforeFall(EntityUid uid, PreventChasmFallingComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (TryComp<UseDelayComponent>(uid, out var useDelay) && _delay.IsDelayed((uid, useDelay)))
            return;

        args.Cancelled = true;
        var coords = Transform(args.Entity).Coordinates;
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));

        // tries20 my beloved
        for (int i = 0; i < 20; i++)
        {
            var newCoords = new EntityCoordinates(Transform(args.Entity).ParentUid, coords.X + rand.NextFloat(-5f, 5f), coords.Y + rand.NextFloat(-5f, 5f));
            if (!_interaction.InRangeUnobstructed(args.Entity, newCoords, -1f))
                continue;

            _chasms.Clear();
            _lookup.GetEntitiesInRange(newCoords, 1f, _chasms);
            if (_chasms.Count > 0) // no teleporting onto another chasm
                continue;

            _transform.SetCoordinates(args.Entity, newCoords);
            _transform.AttachToGridOrMap(args.Entity, Transform(args.Entity));
            _audio.PlayLocal(comp.TeleportSound, args.Entity, null);
            if (args.Entity != uid && comp.DeleteOnUse)
                PredictedQueueDel(uid);
            else if (useDelay != null)
                _delay.TryResetDelay((uid, useDelay));

            return;
        }
    }

    private void Relay(EntityUid uid, InventoryComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (!HasComp<ContainerManagerComponent>(uid))
            return;

        RelayEvent(uid, ref args);
    }

    private void RelayEvent(EntityUid uid, ref BeforeChasmFallingEvent ev)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        foreach (var container in containerManager.Containers.Values)
        {
            if (ev.Cancelled)
                break;

            foreach (var entity in container.ContainedEntities)
            {
                RaiseLocalEvent(entity, ref ev);
                if (ev.Cancelled)
                    break;
                RelayEvent(entity, ref ev);
            }
        }
    }
}
