using Robust.Shared.Prototypes;

namespace Content.Shared._Starfall.Particles;

/// <summary>
/// Spawns a particle effect when specific events fire on this entity.
/// Particles with infinite durations cannot be used with this.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnEventComponent : Component
{
    /// <summary>The particle effect to spawn. Particles with infinite durations cannot be used.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    /// <summary>Optional color tint applied to particles.</summary>
    [DataField]
    public Color? ColorOverride;

    /// <summary>Spawn particles when this entity is used in-hand (flashlight toggled).</summary>
    [DataField]
    public bool OnUse;

    /// <summary>Spawn particles when this entity is used on a target (item applied to something).</summary>
    [DataField]
    public bool OnUseInWorld;

    /// <summary>Spawn particles when this entity attacks with melee.</summary>
    [DataField]
    public bool OnMeleeAttack;

    /// <summary>Spawn particles on each entity this weapon hits with melee (spawns on the victims, not the attacker).</summary>
    [DataField]
    public bool OnMeleeAttackOther;

    /// <summary>Spawn particles when this entity is hit by melee.</summary>
    [DataField]
    public bool OnMeleeHit;

    /// <summary>Spawn particles when this entity is thrown.</summary>
    /// <remarks>
    /// This is an exception to the infinite-duration rule, system will automatically stop the emitter when the entity lands.
    /// This allows for things like a smoke trail on thrown grenades without needing to set a duration on the particle effect.
    /// </remarks>
    [DataField]
    public bool OnThrown;

    /// <summary>Spawn particles when this entity lands after being thrown.</summary>
    [DataField]
    public bool OnLanded;

    /// <summary>Spawn particles when this item is primed/activated (timed explosives, etc).</summary>
    [DataField]
    public bool OnPrimed;

    /// <summary>Spawn particles when this gun fires (muzzle flash, etc).</summary>
    [DataField]
    public bool OnGunShot;

    /// <summary>Spawn particles on each projectile fired by this gun.</summary>
    /// <remarks>
    /// This is an exception to the infinite-duration rule, projectile trails CAN use infinite effects
    /// because they're destroyed automatically when the projectile despawns/hits something.
    /// </remarks>
    [DataField]
    public bool OnGunShotProjectile;

    /// <summary>Spawn particles when this projectile hits something (spawns on the projectile).</summary>
    [DataField]
    public bool OnProjectileHit;

    /// <summary>Spawn particles on whatever this projectile hits (spawns on the victim).</summary>
    [DataField]
    public bool OnProjectileHitOther;
}

