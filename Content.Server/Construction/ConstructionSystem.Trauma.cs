using System.Linq;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction;

/// <summary>
/// Trauma - helper for shared code to call server methods
/// </summary>
public sealed partial class ConstructionSystem
{
    public override bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => ChangeNode(uid, userUid, id, performActions, null);

    public bool CheckConstructionKnowledge(EntityUid user, Dictionary<EntProtoId, int> userConstructionGroup, ConstructionPrototype constructionPrototype, string prototype)
    {
        if (TryComp<KnowledgeHolderComponent>(user, out _) && !constructionPrototype.Groups.Keys.All(group => userConstructionGroup.ContainsKey(group)))
        {
            Log.Error($"User {ToPrettyString(user)} tried to start a construction {prototype} that it doesn't have knowledge about!");
            return false;
        }
        return true;
    }

    public void EnsureConstructionKnowledge(EntityUid item, ConstructionPrototype constructionPrototype)
    {
        EnsureComp<KnowledgeConstructionModifierComponent>(item, out var knowledgeConstructionModifier);
        foreach (var construct in constructionPrototype.Groups)
        {
            knowledgeConstructionModifier.LevelDeltas[construct.Key] = construct.Value;
        }
    }
}
