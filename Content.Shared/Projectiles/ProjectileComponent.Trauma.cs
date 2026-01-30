using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Projectiles;

/// <summary>
/// Trauma - extensions to projectile for damage and targeting changes.
/// </summary>
public sealed partial class ProjectileComponent
{
    /// <summary>
    /// When <see cref="IgnoreResistances"/> is false, only allow modifier events to increase damage.
    /// </summary>
    [DataField]
    public bool IncreaseOnly;

    [DataField]
    public bool Penetrate;

    /// <summary>
    ///     Collision mask of what not to penetrate if <see cref="Penetrate"/> is true.
    /// </summary>
    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>))]
    public int NoPenetrateMask = 0;

    [NonSerialized]
    public List<EntityUid> IgnoredEntities = new();

    [DataField]
    public Vector2 TargetCoordinates;
}
