using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Construction;

/// <summary>
/// Trauma - virtual methods for calling from shared code
/// </summary>
public abstract partial class SharedConstructionSystem
{
    public virtual bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => false;

    /// <summary>
    /// Trauma - Returns all available construction groups for that entity.
    /// </summary>
    public Dictionary<EntProtoId, int> AvailableConstructionGroups(EntityUid user)
    {
        var ev = new ConstructionGetGroupsEvent(new());
        RaiseLocalEvent(user, ref ev);
        Log.Debug($"Available construction groups for {user}: {string.Join(", ", ev.Groups.Select(g => g.Key))}");
        Log.Debug($"ConstructionGetGroupsEvent groups collected for entity {ToPrettyString(user)}, groups count: {ev.Groups.Count()}");
        return ev.Groups;
    }

    public bool IsKnowledgeHolder(EntityUid user)
    {
        return HasComp<KnowledgeHolderComponent>(user);
    }
}
