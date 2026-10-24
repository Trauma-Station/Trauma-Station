// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;

namespace Content.Trauma.Shared.Item;

/// <summary>
/// Sends a <see cref="BindItemEvent"/> to the wearer of an item with
/// <see cref="BindItemOnPickupComponent"/>.
/// </summary>
public sealed partial class BindItemOnPickupSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnPickedUp(Entity<BindItemOnPickupComponent> ent, ref GotEquippedHandEvent args)
    {
        var ev = new BindItemEvent(ent.Owner);
        RaiseLocalEvent(args.User, ref ev);
    }
}

/// <summary>
/// Raised on a wearer when an equipped item requests to be bound to them.
/// </summary>
/// <remarks>
/// The receiving system defines what binding means. This event only identifies the
/// <see cref="Item"/> and does not enforce ownership or replacement behavior.
/// </remarks>
[ByRefEvent]
public readonly record struct BindItemEvent(EntityUid Item);
