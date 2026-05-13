// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Common.Grab;

public abstract partial class CommonGrabThrownSystem : EntitySystem
{
    /// <summary>
    /// Checks to see if the entity is a thrown entity. Returns true if entity is thrown.
    /// </summary>
    public abstract bool IsGrabThrown(EntityUid thrown);

    /// <summary>
    /// Throwing entity to the direction and ensures GrabThrownComponent with params
    /// </summary>
    /// <param name="uid">Entity to throw</param>
    /// <param name="thrower">Entity that throws</param>
    /// <param name="vector">Direction</param>
    /// <param name="grabThrownSpeed">How fast you fly when thrown</param>
    /// <param name="staminaDamage">Stamina damage on collide</param>
    /// <param name="damage">Damage to apply on collide</param>
    public abstract void Throw(
        EntityUid uid,
        EntityUid thrower,
        Vector2 vector,
        float grabThrownSpeed,
        DamageSpecifier? damage = null,
        bool drop = true);
}
