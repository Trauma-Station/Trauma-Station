// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

[ByRefEvent]
public readonly record struct ComplexJointUpdateEvent(EntityUid Uid, Dictionary<string, HashSet<EntityUid>> UpdatedIds);

[ByRefEvent]
public readonly record struct ComplexJointCollisionEvent(
    EntityUid Origin,
    RayCastResults Hit,
    EntityUid Target,
    ComplexJointVisualsData Data);

[ByRefEvent]
public record struct BeforeContinuousBeamDamagedEvent(EntityUid Uid, EntityUid Target, bool Cancelled = false);

[ByRefEvent]
public readonly record struct AfterContinuousBeamDamagedEvent(EntityUid Uid, EntityUid Target);

[ByRefEvent]
public readonly record struct ContinuousBeamStoppedFiringEvent;

[ByRefEvent]
public record struct BeforeContinuousBeamDamageTickEvent(
    Entity<ContinuousBeamGunComponent, ComplexJointVisualsComponent> Ent,
    bool Cancelled = false);
