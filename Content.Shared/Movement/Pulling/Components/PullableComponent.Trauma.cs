using Content.Trauma.Common.MartialArts;

namespace Content.Shared.Movement.Pulling.Components;

public sealed partial class PullableComponent
{
    [DataField]
    public Dictionary<GrabStage, short> PulledAlertAlertSeverity = new()
    {
        { GrabStage.No, 0 },
        { GrabStage.Soft, 1 },
        { GrabStage.Hard, 2 },
        { GrabStage.Suffocate, 3 },
    };

    [AutoNetworkedField, DataField]
    public GrabStage GrabStage = GrabStage.No;

    [AutoNetworkedField, DataField]
    public float GrabEscapeChance = 1f;

    [AutoNetworkedField]
    public TimeSpan NextEscapeAttempt = TimeSpan.Zero;

    /// <summary>
    /// If this pullable being tabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BeingTabled = false;

    /// <summary>
    /// Constant for tabling throw math
    /// </summary>
    [DataField]
    public float BasedTabledForceSpeed = 5f;

    /// <summary>
    ///  Stamina damage. taken on tabled
    /// </summary>
    [DataField]
    public float TabledStaminaDamage = 40f;

    /// <summary>
    /// Damage taken on being tabled.
    /// </summary>
    [DataField]
    public float TabledDamage = 5f;

    [DataField]
    public float EscapeAttemptCooldown = 2f;

    [DataField]
    public bool CanBeGrabbed = true;
}
