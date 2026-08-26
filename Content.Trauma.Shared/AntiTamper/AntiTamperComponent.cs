using Robust.Shared.Audio;

namespace Content.Trauma.Shared.AntiTamper;

[RegisterComponent]
public sealed partial class AntiTamperComponent : Component
{
    [DataField]
    public bool Enabled = true;

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

    [DataField]
    public SoundPathSpecifier? AlarmSound;

    [DataField]
    public TimeSpan YellCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan AlarmCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan LastYell = TimeSpan.Zero;

    [DataField]
    public TimeSpan LastAlarm = TimeSpan.Zero;

    /// <summary>
    /// Message yelled, see <see cref="YellAlertType"/>. 
    /// </summary>
    [DataField]
    public LocId LocTamperMessage = "anti-tamper-damaged";
};