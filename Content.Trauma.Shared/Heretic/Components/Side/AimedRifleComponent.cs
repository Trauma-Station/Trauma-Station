// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components.Side;

/// <summary>
/// Adds "aiming" functionality with do-after for a gun when right-clicking target in combat mode
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AimedRifleComponent : Component
{
    /// <summary>
    /// How long does aiming take per tile between us and target
    /// </summary>
    [DataField]
    public TimeSpan AimTimePerDistance = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Max aim du-after time
    /// </summary>
    [DataField]
    public TimeSpan MaxAimTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// If distance is less than this, we can't aim
    /// </summary>
    [DataField]
    public float MinDistance = 4f;

    /// <summary>
    /// If distance is greater than this, we can't aim
    /// </summary>
    [DataField]
    public float MaxDistance = 30f;

    /// <summary>
    /// Target we are currently aiming at
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AimingAt;

    /// <summary>
    /// Determines whether we can aim at target
    /// </summary>
    [DataField]
    public EntityWhitelist? AimWhitelist;

    /// <summary>
    /// Use delay id that prevents aiming immediately after aiming shot
    /// </summary>
    [DataField]
    public string AimUseDelayId = "aim";

    /// <summary>
    /// Whether Target will be visually marked with <see cref="AimedRifleMarkerComponent"/> when aiming at it
    /// </summary>
    [DataField]
    public bool ShowMark = true;
}
