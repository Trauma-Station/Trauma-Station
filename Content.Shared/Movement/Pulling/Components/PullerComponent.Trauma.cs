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
}
