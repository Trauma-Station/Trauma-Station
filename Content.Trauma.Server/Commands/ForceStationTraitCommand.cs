// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Trauma.Server.Station;
using Robust.Shared.Console;

namespace Content.Trauma.Server.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ForceStationTraitCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private StationTraitsSystem? _traits;

    public string Command => "forcestationtrait";
    public string Description => "Forces a station trait to be picked when the next round starts, only for the next round";
    public string Help => "forcestationtrait [id]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Wrong number of arguments");
            return;
        }

        _traits ??= _entMan.System<StationTraitsSystem>();

        var id = args[0];
        shell.WriteLine(_traits.ForceTrait(id)
            ? $"Trait {id} will be forced for the next round"
            : $"Unknown station trait ID '{id}'");
    }
}
