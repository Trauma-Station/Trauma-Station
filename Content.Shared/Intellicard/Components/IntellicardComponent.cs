// <Trauma>
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
// </Trauma>
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Intellicard;

/// <summary>
/// Allows this entity to download the station AI onto an AiHolderComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause] // Trauma
public sealed partial class IntellicardComponent : Component
{
    /// <summary>
    /// The duration it takes to download the AI from an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DownloadTime = 15;

    /// <summary>
    /// The duration it takes to upload the AI to an AiHolder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UploadTime = 3;

    /// <summary>
    /// Corvax - The sound that plays for the AI when they are being downloaded.
    /// </summary>
    [DataField]
    public SoundSpecifier? WarningSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    /// Corvax - The delay before allowing the warning to play again in seconds.
    /// </summary>
    [DataField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Corvax - When a card warning is allowed to be played again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextWarningAllowed = TimeSpan.Zero;
}
