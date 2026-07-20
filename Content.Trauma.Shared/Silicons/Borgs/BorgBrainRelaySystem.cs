// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Common.Silicons.Borgs;

namespace Content.Trauma.Shared.Silicons.Borgs;

/// <summary>
/// Relays borg brain events from MMI to the contained brain, if it has one.
/// </summary>
public sealed class BorgBrainRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIComponent, BorgBrainInsertedEvent>(RelayEvent);
        SubscribeLocalEvent<MMIComponent, BorgBrainRemovedEvent>(RelayEvent);
    }

    private void RelayEvent<T>(Entity<MMIComponent> ent, ref T args) where T: notnull
    {
        if (ent.Comp.BrainSlot.ContainerSlot?.ContainedEntity is {} brain)
            RaiseLocalEvent(brain, ref args);
    }
}
