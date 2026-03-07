using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Reflect;

/// <summary>
/// Applied to an entity if it reflects an attack using an item with a <see cref="SkillBasedReflectComponent" />.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class SkillBasedReflectExhaustionComponent : Component
{
    /// <summary>
    /// Current exhaustion. Reduces reflect chance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Exhaustion;

    /// <summary>
    /// How fast exhaustion is regenerated when not being attacked, per second. Affected by melee skill.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ExhaustionRegenRate = 0.5f;

    /// <summary>
    /// How much time must pass since last reflect attempt in order to start reducing exhaustion. Affected by melee skill.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExhaustionRegenDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExhaustionRegenTimer = default!;
}
