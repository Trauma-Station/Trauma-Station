using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Marks an entity to take additional damage
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedDamageMarkerSystem))] // Lavaland - raise auto handle state event
[AutoGenerateComponentPause]
public sealed partial class DamageMarkerComponent : Component
{
    /// <summary>
    /// Sprite to apply to the entity while damagemarker is applied.
    /// </summary>
    [DataField, AutoNetworkedField] // Lavaland - networked and cleanup
    public SpriteSpecifier.Rsi? Effect; // Lavaland - null by default

    /// <summary>
    /// Sound to play when the damage marker is procced.
    /// </summary>
    [DataField, AutoNetworkedField] // Lavaland - networked and cleanup
    public SoundSpecifier? Sound; // Lavaland - null by default

    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entity that marked this entity for a damage surplus.
    /// </summary>
    [DataField, AutoNetworkedField] // Lavaland - networked and cleanup
    public EntityUid Marker;

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField] // Lavaland - networked and cleanup
    [AutoPausedField]
    public TimeSpan EndTime;
}
