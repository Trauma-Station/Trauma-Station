// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Console;

namespace Content.Trauma.Client.Areas;

public sealed class ShowAreasCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AreaVisibilitySystem _areaVisibility = default!;

    public override string Command => "showareas";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _areaVisibility.ToggleVisibility();
    }
}
