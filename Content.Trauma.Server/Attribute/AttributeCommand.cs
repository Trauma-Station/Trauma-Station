// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Attribute.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Trauma.Server.Attribute;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class AttributeCommand : ToolshedCommand
{
    private SharedAttributeSystem? _attribute;

    [CommandImplementation("add")]
    public EntityUid Add([PipedArgument] EntityUid input, [CommandArgument] EntProtoId proto, [CommandArgument] FixedPoint2 level)
    {
        _attribute ??= GetSys<SharedAttributeSystem>();

        if (_attribute.GetContainer(input) is { } brain)
            _attribute.EnsureAttribute(brain, proto, level);
        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> Add([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] EntProtoId proto, [CommandArgument] FixedPoint2 level)
        => input.Select(x => Add(x, proto, level));

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List([PipedArgument] IEnumerable<EntityUid> entities)
    {
        _attribute ??= GetSys<SharedAttributeSystem>();

        return entities.SelectMany(e =>
        {
            var units = _attribute.TryGetAllAttributeUnits(e);

            if (units == null)
                return Array.Empty<EntityUid>();

            return units.Select(u => u.Owner);
        });
    }

    [CommandImplementation("clear")]
    public EntityUid Clear([PipedArgument] EntityUid input)
    {
        _attribute ??= GetSys<SharedAttributeSystem>();

        _attribute.ClearAttribute(input, true);

        return input;
    }

    [CommandImplementation("clear")]
    public IEnumerable<EntityUid> Clear([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Clear);
}
