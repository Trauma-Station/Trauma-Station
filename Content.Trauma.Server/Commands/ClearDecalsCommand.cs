// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Decals;
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

    private DecalSystem? _decal;
    private List<uint> _decals = new();

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0)
        {
            shell.WriteLine("This command does not accept any arguments");
            return;
        }
        _decal ??= _entMan.System<DecalSystem>();

        var query = _entMan.EntityQueryEnumerator<DecalGridComponent>();
        while (query.MoveNext(out var gridUid, out var decal))
        {
            _decals.Clear();
            foreach (var id in decal.DecalIndex.Keys)
            {
                _decals.Add(id);
            }

            foreach (var id in _decals)
            {
                _decal.RemoveDecal(gridUid, id, decal);
            }
        }
    }
}
