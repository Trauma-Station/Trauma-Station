// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.DeadmanSwitch;

/// <summary>
/// Component holding the state of a deadman's switch, which is mainly 'armed' or 'disarmed'.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeadmanSwitchComponent : Component
{
    public const string DroppedPort = "Dropped";

    /// <summary>
    /// Whether the switch is armed or not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Armed;

    /// <summary>
    /// How long it takes to arm / disarm it
    /// </summary>
    [DataField]
    public TimeSpan ArmDelay;

    /// <summary>
    /// The sound the switch makes when it flips on or off
    /// </summary>
    [DataField]
    public SoundSpecifier? SwitchSound;

    /// <summary>
    /// At this distance, the deadman's switch triggers linked explosives instantly, bypassing timers.
    /// </summary>
    [DataField]
    public float InstantTriggerRange;
}
