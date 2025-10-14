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

    private EntProtoId<MutationComponent> Check(string id)
    {
        var mid = (EntProtoId<MutationComponent>) id;
        if (!Mutation.AllMutations.ContainsKey(mid))
            throw new Exception($"Invalid mutation {id}");
        return mid;
    }
}
