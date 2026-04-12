using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     This component adds a white overlay to the viewport. It does not actually cause blurring.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BlurryVisionSystem))]
public sealed partial class BlurryVisionComponent : Component
{
    /// <summary>
    ///     Amount of "blurring". Also modifies examine ranges.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("magnitude"), AutoNetworkedField]
    public float Magnitude = 3f; // Trauma - add default value

    /// <summary>
    ///     Exponent that controls the magnitude of the effect.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("correctionPower"), AutoNetworkedField]
    public float CorrectionPower = DefaultCorrectionPower; // Trauma - add default value

    public const float MaxMagnitude = 6;
    public const float DefaultCorrectionPower = 2f;
}
