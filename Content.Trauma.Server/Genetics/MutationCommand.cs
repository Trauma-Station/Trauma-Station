// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using System.Text;

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

    private StringBuilder _dump = new();

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

    [CommandImplementation("dump")]
    public string Dump([PipedArgument] EntityUid uid)
    {
        if (Mutation.GetMutatable(uid, true) is not {} ent)
            return Loc.GetString("generic-not-available-shorthand");

        _dump.Clear();
        if (ent.Comp.Mutations.Count > 0)
        {
            _dump.Append("Active Mutations:");
            foreach (var (id, mutation) in ent.Comp.Mutations)
            {
                DumpMutation(id);
                _dump.Append(" - ");
                _dump.Append(EntityManager.ToPrettyString(mutation));
            }
        }
        if (ent.Comp.Dormant.Count > 0)
        {
            _dump.Append("\nDormant Mutations:");
            foreach (var id in ent.Comp.Dormant)
            {
                DumpMutation(id);
            }
        }
        return _dump.ToString();
    }

    private void DumpMutation(EntProtoId<MutationComponent> id)
    {
        _dump.Append("\n- ");
        _dump.Append(id);
        _dump.Append(" (");
        if (Mutation.GetRoundData(id) is {} data)
            _dump.Append(data.Number);
        else
            _dump.Append("???");
        _dump.Append(")");
    }

    private EntProtoId<MutationComponent> Check(string id)
    {
        var mid = (EntProtoId<MutationComponent>) id;
        if (!Mutation.AllMutations.ContainsKey(mid))
            throw new Exception($"Invalid mutation {id}");
        return mid;
    }
}
