using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// Add this component to an entity that you want to be cloaked.
/// It overlays a shader on the entity to give them an invisibility cloaked effect.
/// It also turns the entity invisible.
/// Use other components (like StealthOnMove) to modify this component's visibility based on certain conditions.
/// </summary>
[RegisterComponent, NetworkedComponent]
// Trauma - no access
[AutoGenerateComponentState] // Trauma - replaced manual state handling
public sealed partial class StealthComponent : Component
{
    /// <summary>
    /// Whether or not the stealth effect should currently be applied.
    /// </summary>
    [DataField]
    [AutoNetworkedField] // Trauma
    public bool Enabled = true;

    /// <summary>
    /// The creature will continue invisible at death.
    /// </summary>
    [DataField]
    public bool EnabledOnDeath = true;

    /// <summary>
    /// Whether or not the entity previously had an interaction outline prior to cloaking.
    /// </summary>
    [DataField]
    public bool HadOutline;

    /// <summary>
    /// Minimum visibility before the entity becomes unexaminable (and thus no longer appears on context menus).
    /// </summary>
    [DataField]
    [AutoNetworkedField] // Trauma
    public float ExamineThreshold = 0.5f;

    /// <summary>
    /// Last set level of visibility. The visual effect ranges from 1 (fully visible) and -1.5 (fully hidden). Values
    /// outside of this range simply act as a buffer for the visual effect (i.e., a delay before turning invisible). To
    /// get the actual current visibility, use <see cref="SharedStealthSystem.GetVisibility(EntityUid, StealthComponent?)"/>
    /// If you don't have anything else updating the stealth, this will just stay at a constant value, which can be useful.
    /// </summary>
    [DataField]
    [Access(typeof(SharedStealthSystem), Other = AccessPermissions.None)]
    [AutoNetworkedField] // Trauma
    public float LastVisibility = 1;

    /// <summary>
    /// Time at which <see cref="LastVisibility"/> was set. Null implies the entity is currently paused and not
    /// accumulating any visibility change.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField] // Trauma
    public TimeSpan? LastUpdated;

    // Goobstation - Proper invisibility
    /// <summary>
    /// Minimum visibility. Note that the visual effect caps out at -1.5, but this value is allowed to be larger or smaller.
    /// </summary>
    [DataField]
    [AutoNetworkedField] // Trauma
    public float MinVisibility = -1.5f; // Trauma - was -1

    /// <summary>
    /// Maximum visibility. Note that the visual effect caps out at +1, but this value is allowed to be larger or smaller.
    /// </summary>
    [DataField]
    [AutoNetworkedField] // Trauma
    public float MaxVisibility = 1.5f;

    /// <summary>
    /// The frequency of the shimmer effect. 0 disables the shimmering, leaving only a static distortion.
    /// </summary>
    [DataField]
    [AutoNetworkedField] // Trauma
    public float ShimmerFrequency = 1f;

    /// <summary>
    /// Localization string for how you'd like to describe this effect.
    /// </summary>
    [DataField]
    public string ExaminedDesc = "stealth-visual-effect";
}
