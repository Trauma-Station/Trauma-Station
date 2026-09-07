using Content.Trauma.Common.MartialArts;

namespace Content.Shared.Movement.Pulling.Components;

public sealed partial class PullerComponent : Component
{
    /// <summary>
    /// Whether or not to apply speed modifiers to the puller
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool ApplySpeedModifier = true;

    /// <summary>
    /// allows entities to hardgrab instantaneoulsy instead of progressing to it
    /// </summary>
    [DataField]
    public GrabStage StartingGrabStage = GrabStage.Soft;

    [DataField]
    public Dictionary<GrabStage, short> PullingAlertSeverity = new()
    {
        { GrabStage.No, 0 },
        { GrabStage.Soft, 1 },
        { GrabStage.Hard, 2 },
        { GrabStage.Suffocate, 3 },
    };

    [DataField, AutoNetworkedField]
    public GrabStage GrabStage = GrabStage.No;

    [DataField, AutoNetworkedField]
    public GrabStageDirection GrabStageDirection = GrabStageDirection.Increase;

    [AutoNetworkedField]
    public TimeSpan NextStageChange;

    [DataField]
    public TimeSpan StageChangeCooldown = TimeSpan.FromSeconds(1f);

    [DataField]
    public Dictionary<GrabStage, float> EscapeChances = new()
    {
        { GrabStage.No, 1f },
        { GrabStage.Soft, 1f },
        { GrabStage.Hard, 0.6f },
        { GrabStage.Suffocate, 0.2f },
    };

    [DataField]
    public float SuffocateGrabStaminaDamage = 10f;

    [DataField]
    public float GrabThrowDamageModifier = 2f;

    [ViewVariables]
    public List<EntityUid> GrabVirtualItems = new();

    [ViewVariables]
    public Dictionary<GrabStage, int> GrabVirtualItemStageCount = new()
    {
        { GrabStage.Suffocate, 1 },
    };

    [DataField]
    public float GrabThrownSpeed = 7f;

    [DataField]
    public float ThrowingDistance = 4f;

    [DataField]
    public float SoftGrabSpeedModifier = 0.9f;

    [DataField]
    public float HardGrabSpeedModifier = 0.7f;

    [DataField]
    public float ChokeGrabSpeedModifier = 0.4f;
}
