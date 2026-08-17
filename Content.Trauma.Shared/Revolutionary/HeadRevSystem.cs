// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Trauma.Common.Mindshield;
using Content.Trauma.Shared.Mindshield;

namespace Content.Trauma.Shared.Revolutionary;

/// <summary>
/// Handles headrev mindshield interaction with it disabling conversion ability.
/// </summary>
public sealed partial class HeadRevSystem : EntitySystem
{
    [Dependency] private RevolutionarySystem _rev = default!;

    [SubscribeLocalEvent]
    private void OnMindShieldAttempt(Entity<HeadRevolutionaryComponent> ent, ref MindShieldAttemptEvent args)
    {
        args.CancelPopup = "head-rev-break-mindshield";
    }

    [SubscribeLocalEvent]
    private void OnMindShielded(Entity<HeadRevolutionaryComponent> ent, ref MindShieldedEvent args)
    {
        _rev.SetConvertAbility(ent, false);
    }

    [SubscribeLocalEvent]
    private void OnMindShieldRemoved(Entity<HeadRevolutionaryComponent> ent, ref MindShieldRemovedEvent args)
    {
        _rev.SetConvertAbility(ent, true);
    }
}
