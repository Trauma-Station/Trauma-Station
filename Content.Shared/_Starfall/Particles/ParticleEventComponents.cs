using Robust.Shared.Prototypes;

namespace Content.Shared._Starfall.Particles;

/// <summary>
/// Base class for all particle-on-event components.
/// Provides the shared <see cref="Effect"/> and optional <see cref="ColorOverride"/> fields.
/// </summary>
public abstract partial class ParticleOnEventBase : Component
{
    /// <summary>The particle effect to spawn.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    /// <summary>Optional color tint applied to the spawned particles.</summary>
    [DataField]
    public Color? ColorOverride;
}

/// <summary>Spawns a particle effect when this entity is used in-hand (e.g. flashlight toggle).</summary>
[RegisterComponent]
public sealed partial class ParticleOnUseComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect when this entity is used on a target in the world.</summary>
[RegisterComponent]
public sealed partial class ParticleOnUseInWorldComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on the attacker when this entity attacks with melee.</summary>
[RegisterComponent]
public sealed partial class ParticleOnMeleeAttackComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on each entity hit by a melee attack with this weapon.</summary>
[RegisterComponent]
public sealed partial class ParticleOnMeleeAttackOtherComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on this entity when it is hit by a melee attack.</summary>
[RegisterComponent]
public sealed partial class ParticleOnMeleeHitComponent : ParticleOnEventBase
{
}

/// <summary>
/// Spawns a particle effect while this entity is in flight after being thrown.
/// Unlike most particle event components, infinite-duration effects are allowed here
/// because the system automatically stops the emitter when the entity lands.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnThrownComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect when this entity lands after being thrown.</summary>
[RegisterComponent]
public sealed partial class ParticleOnLandedComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect when this entity is primed/activated (timed explosives, etc.).</summary>
[RegisterComponent]
public sealed partial class ParticleOnPrimedComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on the gun when it fires.</summary>
[RegisterComponent]
public sealed partial class ParticleOnGunShotComponent : ParticleOnEventBase
{
}

/// <summary>
/// Spawns a particle effect on each projectile fired by this gun.
/// Infinite-duration effects are permitted here because the emitter is destroyed
/// when the projectile despawns or hits something.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnGunShotProjectileComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on this projectile when it hits something.</summary>
[RegisterComponent]
public sealed partial class ParticleOnProjectileHitComponent : ParticleOnEventBase
{
}

/// <summary>Spawns a particle effect on whatever this projectile hits.</summary>
[RegisterComponent]
public sealed partial class ParticleOnProjectileHitOtherComponent : ParticleOnEventBase
{
}
