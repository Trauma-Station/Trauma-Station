// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Trauma.Client.Commands;

[AnyCommand]
public sealed partial class CheckDelayCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;

    public string Command => "checkdelay";
    public string Description => "Measures bidirectional delay to/from the server";
    public string Help => "checkdelay";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entMan.System<CheckDelaySystem>().CheckDelay();
    }
}
