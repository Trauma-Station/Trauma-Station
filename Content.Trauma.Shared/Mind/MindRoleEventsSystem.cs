// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Trauma.Common.Roles;

namespace Content.Trauma.Shared.Mind;

/// <summary>
/// Handles raising some events on roles when mind changes.
/// </summary>
public sealed class MindRoleEventSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(OnAdded);
        SubscribeLocalEvent<MindComponent, BeforeMindGotRemovedEvent>(OnBeforeRemoved);
    }

    private void OnAdded(Entity<MindComponent> ent, ref MindGotAddedEvent args)
    {
        if (ent.Comp.OwnedEntity is not {} mob)
            return;

        // tell roles that their mind got added to a mob
        var ev = new RoleMindAddedEvent(ent, mob);
        foreach (var role in ent.Comp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, ref ev);
        }
    }

    private void OnBeforeRemoved(Entity<MindComponent> ent, ref BeforeMindGotRemovedEvent args)
    {
        // this will be the old entity we want, its why BeforeMindGotRemovedEvent is used
        if (ent.Comp.OwnedEntity is not {} mob)
            return;

        // tell roles that their mind got removed from a mob
        var ev = new RoleMindRemovedEvent(ent, mob);
        foreach (var role in ent.Comp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, ref ev);
        }
    }
}
