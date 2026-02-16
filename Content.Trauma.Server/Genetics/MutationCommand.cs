// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Trauma.Server.Genetics;

/// <summary>
/// Mutation toolshed commands.
/// </summary>
/// <example>
/// <c>self mutation:add MutationClumsiness</c>
/// <c>self mutation:add MutationGlowy</c>
/// </example>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class MutationCommand : ToolshedCommand
{
    private MutationSystem? _mutation;
    private MutationSystem Mutation
    {
        get
        {
            _mutation ??= GetSys<MutationSystem>();
            return _mutation;
        }
    }

    [CommandImplementation("add")]
    public void Add(
        [PipedArgument] EntityUid uid,
        [CommandArgument] EntProtoId id)
    {
        Mutation.AddMutation(uid, Check(id));
    }

    [CommandImplementation("remove")]
    public void Remove(
        [PipedArgument] EntityUid uid,
        [CommandArgument] EntProtoId id)
    {
        Mutation.RemoveMutation(uid, Check(id));
    }

    [CommandImplementation("clear")]
    public void Clear([PipedArgument] EntityUid uid)
    {
        if (Mutation.GetMutatable(uid, true) is {} ent)
            Mutation.ClearMutations(ent.AsNullable());
    }

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List([PipedArgument] EntityUid uid)
        => Mutation.GetMutatable(uid, true) is {} ent
            ? ent.Comp.Mutations.Values
            : [];

    [CommandImplementation("dormant")]
    public IEnumerable<EntProtoId<MutationComponent>> Dormant([PipedArgument] EntityUid uid)
        => Mutation.GetMutatable(uid, true) is {} ent
            ? ent.Comp.Dormant
            : [];

    [CommandImplementation("scramble")]
    public void Scramble([PipedArgument] EntityUid uid)
    {
        if (Mutation.GetMutatable(uid, true) is not {} ent)
            return;

        Mutation.Scramble(ent);
    }

    private EntProtoId<MutationComponent> Check(string id)
    {
        var mid = (EntProtoId<MutationComponent>) id;
        if (!Mutation.AllMutations.ContainsKey(mid))
            throw new Exception($"Invalid mutation {id}");
        return mid;
    }
}
