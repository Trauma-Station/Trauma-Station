using Content.Shared.Construction.Prototypes;
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
        return ev.Groups;
    }

    /// <summary>
    /// Returns true if the user knows how to construction. False if not. Logs error too.
    /// </summary>
    public bool CheckConstructionKnowledge(EntityUid user, ConstructionPrototype prototype, bool log = true)
    {
        if (!HasComp<KnowledgeHolderComponent>(user))
            return true; // don't care

        // TODO: just have this be an event the system can cancel
        var skills = AvailableConstructionGroups(user);
        if (CheckConstructionGroups(skills, prototype))
            return true;
        if (log)
            Log.Error($"User {ToPrettyString(user)} tried to start a construction {prototype.ID} that it doesn't have required knowledge for!");
        return false;
    }

    public bool CheckConstructionGroups(Dictionary<EntProtoId, int> skills, ConstructionPrototype prototype)
    {
        foreach (var (id, level) in prototype.Groups)
        {
            if (skills.GetValueOrDefault(id) < level)
                return false;
        }

        return true;
    }

    public bool IsKnowledgeHolder(EntityUid user)
    {
        return HasComp<KnowledgeHolderComponent>(user);
    }
}
