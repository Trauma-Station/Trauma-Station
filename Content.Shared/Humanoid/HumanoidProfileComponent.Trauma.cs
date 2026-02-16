using Content.Goobstation.Common.Barks;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - store the profile's bark voice
/// </summary>
public sealed partial class HumanoidProfileComponent
{
    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;
}
