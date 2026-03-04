// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class KnowledgeCommand : ToolshedCommand
{
    private CommonKnowledgeSystem? _knowledge;

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid input, [CommandArgument] EntProtoId proto, [CommandArgument] int level)
    {
        _knowledge ??= GetSys<CommonKnowledgeSystem>();

        _knowledge.TryAddKnowledgeUnit(input, (proto, level));
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] EntProtoId proto, [CommandArgument] int level)
        => input.Select(x => Add(x, proto, level));

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List([PipedArgument] IEnumerable<EntityUid> entities)
    {
        _knowledge ??= GetSys<CommonKnowledgeSystem>();

        return entities.SelectMany(e =>
        {
            var units = _knowledge.TryGetAllKnowledgeUnits(e);

            if (units == null)
                return Array.Empty<EntityUid>();

            return units.Select(u => u.Owner);
        });
    }

    [CommandImplementation("clear")]
    public EntityUid Clear([PipedArgument] EntityUid input)
    {
        _knowledge ??= GetSys<CommonKnowledgeSystem>();

        _knowledge.ClearKnowledge(input, true);

        return input;
    }

    [CommandImplementation("clear")]
    public IEnumerable<EntityUid> Clear([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Clear);
}
