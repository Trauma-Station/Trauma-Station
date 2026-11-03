// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

public abstract partial class SharedComplexJointVisualsSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    public void ClearBeamJoints(Entity<ComplexJointVisualsComponent?> ent, string excludedId, EntityUid? target = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        TryGetNetEntity(target, out var netTarget);

        ent.Comp.Data = ent.Comp.Data.Where(x => netTarget is { } t && x.Key != t || x.Value.Id != excludedId)
            .ToDictionary();

        if (ent.Comp.Data.Count == 0)
            RemComp(ent.Owner, ent.Comp);
        else
            Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Adds complex joint comp on uidB and links it to uidA as data dictionary key
    /// </summary>
    public void CreateJoint(EntityUid uidA, EntityUid uidB, ComplexJointVisualsData data)
    {
        data.CreationTime = Timing.CurTime;
        var beam = EnsureComp<ComplexJointVisualsComponent>(uidB);
        beam.Data[GetNetEntity(uidA)] = data;
        Dirty(uidB, beam);
    }

    public Dictionary<NetEntity, ComplexJointVisualsData> GetJointData(ComplexJointVisualsComponent joint,
        string id)
    {
        return joint.Data.Where(x => x.Value.Id == id).ToDictionary();
    }
}
