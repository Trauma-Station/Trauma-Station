// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Shared.Item;

namespace Content.Trauma.Shared.Samurai;

/// <summary>
/// Provides shared Samurai API, stops guns from shooting.
/// </summary>
public sealed partial class SamuraiSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem Popup = default!;
    [Dependency] public EntityQuery<SamuraiComponent> SamuraiQuery = default!;

    [SubscribeLocalEvent]
    private void OnBindItem(Entity<SamuraiComponent> ent, ref BindItemEvent args)
    {
        ent.Comp.EnergyPolearm = args.Item;
        Dirty(ent);
    }

    /// <summary>
    /// Require samurai to fight with HONOR, no guns!
    /// </summary>
    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<SamuraiComponent> ent, ref ShotAttemptedEvent args)
    {
        Popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }

    public bool IsSamurai([NotNullWhen(true)] EntityUid? uid)
    {
        return SamuraiQuery.HasComp(uid);
    }

    /// <summary>
    /// Set the samurai's worn suit entity
    /// </summary>
    public void AssignSuit(Entity<SamuraiComponent> ent, EntityUid? suit)
    {
        if (ent.Comp.Suit == suit)
            return;

        ent.Comp.Suit = suit;
        Dirty(ent, ent.Comp);
    }


}
