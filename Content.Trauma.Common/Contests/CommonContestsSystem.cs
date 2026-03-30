using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Common.Contests;

public abstract partial class CommonContestsSystem : EntitySystem
{
    public abstract float MassContest(PhysicsComponent performerPhysics, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(EntityUid performerUid, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(EntityUid performerUid, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(PhysicsComponent performerPhysics, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f);
}
