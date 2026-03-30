using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
    /// <param name="damageScale">Damage to scale on collide</param>
    public abstract void Throw(
        EntityUid uid,
        EntityUid thrower,
        Vector2 vector,
        float grabThrownSpeed,
        float damageScale = 0,
        bool drop = true);
}
