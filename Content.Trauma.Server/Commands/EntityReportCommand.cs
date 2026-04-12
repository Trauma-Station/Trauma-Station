// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Trauma.Server.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class EntityReportCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "entityreport";
    public string Description => "Get stats for the most common entity prototypes";
    public string Help => "entityreport [top N = 10]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteLine("Too many arguments");
            return;
        }

        var topN = 10;
        if (args.Length > 0)
            topN = int.Parse(args[0]);

        // collect the count of every prototype
        var dict = new Dictionary<string, int>();
        var query = _entMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out _, out var meta))
        {
            var id = meta.EntityPrototype?.ID ?? string.Empty;
            dict[id] = dict.GetValueOrDefault(id) + 1;
        }

        var flattened = new List<(string, int)>(dict.Count);
        var total = 0;
        foreach (var (id, count) in dict)
        {
            flattened.Add((id, count));
            total += count;
        }
        // descending
        flattened.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        shell.WriteLine($"Top {topN} most common entity prototypes ({total} total):");
        for (int i = 1; i <= topN; i++)
        {
            var (id, count) = flattened[i - 1];
            if (id == string.Empty)
                id = "(no prototype)";

            var percent = 100f * (float) count / total;
            shell.WriteLine($"#{i}: {id} - {count} ({percent:0.0}%)");
        }
    }
}
