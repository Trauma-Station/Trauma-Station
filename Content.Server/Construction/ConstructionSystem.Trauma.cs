using System.Linq;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge;
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

    /// <summary>
    /// Returns true on knowing construction. False if not. Logs error too.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="userConstructionGroup"></param>
    /// <param name="constructionPrototype"></param>
    /// <param name="prototype"></param>
    /// <returns></returns>
    public bool CheckConstructionKnowledge(EntityUid user, Dictionary<EntProtoId, int> userConstructionGroup, ConstructionPrototype constructionPrototype, string prototype)
    {
        if (HasComp<KnowledgeHolderComponent>(user) && !constructionPrototype.Groups.Keys.All(group => userConstructionGroup.ContainsKey(group)))
        {
            Log.Error($"User {ToPrettyString(user)} tried to start a construction {prototype} that it doesn't have knowledge about!");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Passes constructionPrototype entiry into the modifier component
    /// </summary>
    /// <param name="item"></param>
    /// <param name="constructionPrototype"></param>
    public void EnsureConstructionKnowledge(EntityUid item, ConstructionPrototype constructionPrototype, EntityUid user)
    {
        EnsureComp<QualityComponent>(item, out var Quality);
        foreach (var construct in constructionPrototype.Groups)
        {
            Quality.LevelDeltas[construct.Key] = construct.Value;
        }
        if (!HasComp<KnowledgeHolderComponent>(user))
            return;
        var ev = new UpdateItemQualityEvent(user);
        RaiseLocalEvent(item, ref ev);
    }

    public void TransferQuality(EntityUid original, EntityUid created)
    {
        if (!TryComp<QualityComponent>(original, out var originalComp))
            return;

        if (TryComp<QualityComponent>(created, out var newComp))
        {
            var quality = newComp.Quality * originalComp.NumberOfMasteries;
            quality += originalComp.Quality;
            newComp.NumberOfMasteries = originalComp.NumberOfMasteries + 1;
            newComp.Quality = quality / newComp.NumberOfMasteries;
            Dirty(created, newComp);
            return;
        }
        newComp = EnsureComp<QualityComponent>(created);
        newComp.LevelDeltas = originalComp.LevelDeltas;
        newComp.Quality = originalComp.Quality;
        newComp.NumberOfMasteries = originalComp.NumberOfMasteries;
        Dirty(created, newComp);
        return;
    }
}
