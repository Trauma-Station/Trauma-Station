// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Robust.Shared.Console;

namespace Content.Trauma.Server.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ClearDecalsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "cleardecals";
    public string Description => "Clear all decals in the game, including uncleanable ones";
    public string Help => "cleardecals";

    private List<EntityUid> _decals = new();

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0)
        {
            shell.WriteLine("This command does not accept any arguments");
            return;
        }

        _decals.Clear();
        var query = _entMan.EntityQueryEnumerator<DecalComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            // TODO: could have an option to leave uncleanable ones
            _decals.Add(uid);
        }

        foreach (var uid in _decals)
        {
            _entMan.DeleteEntity(uid);
        }
    }
}
