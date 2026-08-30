using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AntiTamper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    /// Cooldown between alarm sounds.
    /// </summary>
    [DataField]
    public TimeSpan AlarmCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Last time of the yell.
    /// </summary>
    [DataField]
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
