// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Parry;

/// <summary>
/// Applied to an entity if it reflects an attack using an item with a <see cref="ParryComponent" />.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ParryExhaustionComponent : Component
{
    /// <summary>
    /// Current exhaustion. Goes from 0 to 1.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Exhaustion;

    /// <summary>
    /// How fast exhaustion is regenerated when not being attacked, per second.
    /// </summary>
    [DataField]
    public float ExhaustionRegenRate = 0.1f;

    /// <summary>
    /// How much time must pass since last reflect attempt in order to start reducing exhaustion.
    /// </summary>
    [DataField]
    public TimeSpan ExhaustionRegenDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan ExhaustionRegenTimer;
}
