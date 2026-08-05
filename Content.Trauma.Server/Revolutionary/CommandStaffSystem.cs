// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Revolutionary.Components;
using Content.Trauma.Common.Mindshield;

namespace Content.Trauma.Server.Revolutionary;

public sealed partial class CommandStaffSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnMindShielded(Entity<CommandStaffComponent> ent, ref MindShieldAttemptEvent args)
    {
        ent.Comp.Enabled = true;
    }
}
