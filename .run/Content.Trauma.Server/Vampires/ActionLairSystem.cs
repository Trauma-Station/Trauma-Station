// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Server.Vampires;

public sealed partial class ActionLairSystem : SharedActionLairSystem
{
    [Dependency] private GhostSystem _ghost = default!;

    protected override void GhostBoo(EntityUid uid)
    {
        _ghost.DoGhostBooEvent(uid);
    }
}
