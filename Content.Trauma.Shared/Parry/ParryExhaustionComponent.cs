// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
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
    public float Exhaustion { get; set; }

    /// <summary>
    /// If <see cref="Exhaustion"/> is greater or equal than this, we cannot parry melee attacks
    /// </summary>
    [DataField]
    public float MaxParryExhaustion = 0.25f;

    /// <summary>
    /// If <see cref="Exhaustion"/> is greater or equal than this, we cannot reflect ranged attacks
    /// </summary>
    [DataField]
    public float MaxReflectExhaustion = 1f;

    /// <summary>
    /// How fast exhaustion is regenerated when not being attacked, per second.
    /// </summary>
    [DataField]
    public float ExhaustionRegenRate = 0.1f;

    /// <summary>
    /// How much time must pass since last reflect attempt in order to start reducing exhaustion.
    /// </summary>
    [DataField]
    public TimeSpan ExhaustionRegenDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Alert to show when exhausted
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ParryExhaustion";

    /// <summary>
    /// Alert severity will be set to <see cref="Exhaustion"/> multiplied by this and cast to short
    /// </summary>
    [DataField]
    public float AlertSeverityMultiplier = 4f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan ExhaustionRegenTimer;
}
