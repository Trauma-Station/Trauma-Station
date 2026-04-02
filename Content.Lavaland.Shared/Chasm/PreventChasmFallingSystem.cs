// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Common.Chasm;
using Content.Shared.Chasm;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Lavaland.Shared.Chasm;

public sealed class PreventChasmFallingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventChasmFallingComponent, BeforeChasmFallingEvent>(OnBeforeFall);
        SubscribeLocalEvent<InventoryComponent, BeforeChasmFallingEvent>(Relay);
    }

    private HashSet<Entity<ChasmComponent>> _chasms = new();

    private void OnBeforeFall(EntityUid uid, PreventChasmFallingComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (TryComp<UseDelayComponent>(uid, out var useDelay) && _delay.IsDelayed((uid, useDelay)))
            return;

        args.Cancelled = true;
        var coords = Transform(args.Entity).Coordinates;

        // tries20 my beloved
        const int attempts = 20;
        for (int i = 0; i < attempts; i++)
        {
            // TODO: use predicted random if ChasmSystem gets predicted
            var newCoords = new EntityCoordinates(Transform(args.Entity).ParentUid, coords.X + _random.NextFloat(-5f, 5f), coords.Y + _random.NextFloat(-5f, 5f));
            if (!_interaction.InRangeUnobstructed(args.Entity, newCoords, -1f))
                continue;

            _chasms.Clear();
            _lookup.GetEntitiesInRange(newCoords, 1f, _chasms);
            if (_chasms.Count > 0) // no teleporting onto another chasm
                continue;

            _transform.SetCoordinates(args.Entity, newCoords);
            _transform.AttachToGridOrMap(args.Entity, Transform(args.Entity));
            _audio.PlayPvs(comp.TeleportSound, args.Entity);
            if (args.Entity != uid && comp.DeleteOnUse)
                QueueDel(uid);
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
