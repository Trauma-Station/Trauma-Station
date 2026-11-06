// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Vehicle;

/// <summary>
/// A tow-hitch system for vehicles.
/// By pulling an object, and alt-clicking on the vehicle, you can hitch it to carry it with you.
/// </summary>
public sealed partial class TowHitchSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<TowHitchComponent> _towHitchQuery = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<TowHitchComponent> ent, ref ExaminedEvent args)
    {
        // The only way to let people know how to use this
        args.PushText("This vehicle supports a tow-hitch. Pull an object, and click the verb to hitch it.");
    }

    [SubscribeLocalEvent]
    private void OnAltInteract(Entity<TowHitchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var towHitch = ent.AsNullable();

        // Tow-hitches support only 1 object (makes sense), so you must un-hitch the entity before adding another.
        // This is done via separate verbs.
        if (ent.Comp.Hitching is not { } hitch)
        {
            if (_pulling.GetPulledEntity(user) is not { } pulled || _whitelist.IsWhitelistFail(ent.Comp.Whitelist, pulled))
                return;

            args.Verbs.Add(new AlternativeVerb
            {
                Priority = 100,
                Act = () => Hitch(towHitch, pulled, user),
                Text = $"Hitch {Name(pulled)}",
            });

            return;
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 100,
            Act = () => UnHitch(towHitch, user),
            Text = $"Un-hitch {Name(hitch)}",
        });
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<TowHitchedComponent> ent, ref PullAttemptEvent args)
    {
        // No pulling!
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<TowHitchedComponent> ent, ref EntParentChangedMessage args)
    {
        // In all cases, remove component when we change parents
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnThrow(Entity<TowHitchedComponent> ent, ref ThrownEvent args)
    {
        // Throwing is another edge-case, just un-hitch
        if (ent.Comp.HitchedTo is not { } towHitch)
            return;

        UnHitch(towHitch, null);
    }

    [SubscribeLocalEvent]
    private void OnCollide(Entity<TowHitchedComponent> ent, ref PreventCollideEvent args)
    {
        // Weird shit happens if we collide with the vehicle...
        if (ent.Comp.HitchedTo is not { } towHitch || args.OtherEntity != towHitch)
            return;

        args.Cancelled = true;
    }

    /// <summary>
    /// Hitches an entity on a vehicle, essentially parenting it.
    /// </summary>
    /// <param name="ent">The vehicle</param>
    /// <param name="target">The entity to hitch</param>
    /// <param name="user">The entity that initiated the hitch</param>
    public void Hitch(Entity<TowHitchComponent?> ent, EntityUid target, EntityUid user)
    {
        if (!_towHitchQuery.Resolve(ent.Owner, ref ent.Comp) || TerminatingOrDeleted(target))
            return;

        if (ent.Comp.Hitching is { } hitch)
        {
            // If we already have a hitched entity, do unhitch behavior to allow for the other entity to be hitched
            UnHitch(hitch, user);
            return;
        }

        _popup.PopupEntity($"You hitch {Name(target)}!", user, PopupType.SmallCaution);

        // This also sets the parent to the tow-hitch entity, with a given offset
        var xform = Transform(target);
        _transform.SetCoordinates(target, xform, new EntityCoordinates(ent, ent.Comp.Offset));

        _pulling.StopAllPulls(user, stopPullable: false);

        var hitched = EnsureComp<TowHitchedComponent>(target);
        hitched.HitchedTo = ent;
        ent.Comp.Hitching = target;

        Dirty(ent);
        Dirty(target, hitched);
    }

    /// <summary>
    /// Un-hitches the current hitched entity on the vehicle, and re-attaches it on the map.
    /// </summary>
    /// <param name="ent">The vehicle</param>
    /// <param name="user">The entity that initiated the un-hitch</param>
    public void UnHitch(Entity<TowHitchComponent?> ent, EntityUid? user)
    {
        if (!_towHitchQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Hitching is not { } hitch)
            return;

        if (user is { } usr)
            _popup.PopupEntity($"You un-hitch {Name(hitch)}!", usr, PopupType.SmallCaution);

        _transform.AttachToGridOrMap(hitch);

        ent.Comp.Hitching = null;
        Dirty(ent);
    }
}
