// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics.Components;

namespace Content.Trauma.Common.Contests;

public abstract partial class CommonContestsSystem : EntitySystem
{
    public abstract float MassContest(PhysicsComponent performerPhysics, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(EntityUid performerUid, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(EntityUid performerUid, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float MassContest(PhysicsComponent performerPhysics, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float StaminaContest(EntityUid performer, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float StaminaContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float HealthContest(EntityUid performer, bool bypassClamp = false, float rangeFactor = 1f);
    public abstract float HealthContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f);
}
