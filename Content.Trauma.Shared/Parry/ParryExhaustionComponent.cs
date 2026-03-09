// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
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
    [DataField, AutoNetworkedField]
    public float ExhaustionRegenRate = 0.25f;

    /// <summary>
    /// How much time must pass since last reflect attempt in order to start reducing exhaustion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExhaustionRegenDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExhaustionRegenTimer = default!;
}
