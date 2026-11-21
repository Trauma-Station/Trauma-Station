// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.OrganRot;
using Content.Server.Cargo.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Medical.Server.OrganRot;

/// <summary>
/// goida huinia
/// </summary>
public sealed partial class OrganRotSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private TagSystem _tag = default!;

    private ProtoId<TagPrototype> trashTag = "Trash";

    private void OnOrganRemoved(Entity<ChildOrganComponent> ent, OrganGotRemovedEvent args)
    {
        // i fucking hate this shitcoded shit
        Log.Info($"{ent.Owner}");
        // organ is outside the body and is not blacklisted so rotting logic applies
        if (!TryComp<OrganRotBlacklistComponent>(ent.Owner, out var blacklisted))
            EnsureComp<OrganRotComponent>(ent.Owner);
    }

    private void DamageOrgan(EntityUid uid)
    {
        // only fresh organs can be sold
        if (TryComp<StaticPriceComponent>(uid, out var price))
            price.Price = 0;

        _tag.TryAddTag(uid, trashTag);

        // How am i supposed to damage the organ?
    }

    // why its not working!?
    /*
    error RA0054: Method signature is incompatible with required delegate type(s)
    for "Robust.Shared.Analyzers.SubscribeLocalEventAttribute". Compatible types are:
    Robust.Shared.GameObjects.EntityEventHandler, Robust.Shared.GameObjects.EntityEventRefHandler,
    Robust.Shared.GameObjects.EntitySessionEventHandler, Robust.Shared.GameObjects.ComponentEventHandler,
    Robust.Shared.GameObjects.ComponentEventRefHandler, Robust.Shared.GameObjects.EntityEventRefHandler
    */
    // [SubscribeLocalEvent]
    // private void OnCompAdd(Entity<OrganRotComponent> ent, ComponentStartup args)
    // {
    //     Log.Info($"ent.Comp.RottingInterval {ent.Comp.RottingInterval}");
    //     if (ent.Comp.NexRot == default)
    //         ent.Comp.NexRot = _timing.CurTime + ent.Comp.RottingInterval;

    //     if (ent.Comp.NexRot == TimeSpan.FromSeconds(0))
    //         Log.Info($"pidoras");
    // }

    // how can i make it update every 2 seconds? it does not to be precise
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OrganRotComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // this is shit
            Log.Info($"comp.RottingInterval {comp.RottingInterval}");
            if (comp.NexRot == default)
            {
                comp.NexRot = _timing.CurTime + comp.RottingInterval;
                continue;
            }

            if (comp.NexRot == TimeSpan.FromSeconds(0))
            {
                Log.Info($"pidoras");
                continue;
            }
            // shit ends

            if (_timing.CurTime < comp.NexRot)
                continue;

            // update before temperature check for preserving organ
            comp.NexRot = _timing.CurTime + comp.RottingInterval;

            // atmos check
            if (TryComp<TemperatureComponent>(uid, out var temperature) &&
                temperature.Temperature <= comp.WontRotBelowTemperature)
                continue;

            // freezer check
            if (_container.TryGetOuterContainer(uid, Transform(uid), out var container) &&
                HasComp<AntiRottingContainerComponent>(container.Owner))
                continue;

            DamageOrgan(uid);
        }
    }
}