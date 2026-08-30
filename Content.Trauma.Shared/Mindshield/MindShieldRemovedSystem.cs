// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;

namespace Content.Trauma.Shared.Mindshield;

/// <summary>
/// Raises <see cref="MindShieldRemovedEvent"/> on a mob if its mindshield is removed.
/// </summary>
public sealed partial class MindShieldRemovedSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnImplantRemoved(Entity<MindShieldImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        var ev = new MindShieldRemovedEvent();
        RaiseLocalEvent(args.Implanted, ref ev);
    }
}

[ByRefEvent]
public record struct MindShieldRemovedEvent();
