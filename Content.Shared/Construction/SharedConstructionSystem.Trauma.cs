using System.Linq;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using static Content.Shared.Construction.Prototypes.ConstructionGroupPrototype;

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
    public HashSet<ProtoId<ConstructionGroupPrototype>> AvailableConstructionGroups(EntityUid user)
    {
        var ev = new ConstructionGetGroupsEvent(new());
        RaiseLocalEvent(user, ref ev);
        return ev.Groups;
    }
}

[ByRefEvent]
public record struct ConstructionGetGroupsEvent(HashSet<ProtoId<ConstructionGroupPrototype>> Groups);
