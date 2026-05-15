// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Trauma - check cvar to disable pathfinding
/// </summary>
public sealed partial class PathfindingSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private bool _disabled;

    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, TraumaCVars.DisablePathfinding, x => _disabled = x, true);
    }
}
