// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.AntiTamper;

/// <summary>
/// Allows playing an alarm noise/yelling on this entity being damaged or destroyed, or its AntiTamper wire pulsed/cut (if added).
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AntiTamperComponent : Component
{
    /// <summary>
    /// Whether the AntiTamper feature is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Optional examine for the tamper lock.
    /// </summary>
    [DataField]
    public LocId? LocExamine;

    /// <summary>
    /// When to yell <see cref="LocTamperMessage"/> if at all.
    /// Add RadioMicrophone component to alert on radio aswell.
    /// </summary>
    [DataField(required: true)]
    public AntiTamperAlertType YellAlertType;

    /// <summary>
    /// When to play alarm sound if at all.
    /// </summary>
    [DataField(required: true)]
    public AntiTamperAlertType AlarmAlertType;

    /// <summary>
    /// Alarm sound to play.
    /// </summary>
    [DataField]
    public SoundPathSpecifier? AlarmSound;

    /// <summary>
    /// Cooldown between yells.
    /// </summary>
    [DataField]
    public TimeSpan YellCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Next time it .
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan AlarmCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Last time of the yell.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastYell = TimeSpan.Zero;

    /// <summary>
    /// Last time of the alarm
    /// </summary>
    [DataField]
    public TimeSpan LastAlarm = TimeSpan.Zero;

    /// <summary>
    /// Message yelled, see <see cref="YellAlertType"/>.
    /// </summary>
    [DataField]
    public LocId LocTamperMessage = "anti-tamper-damaged";
};
