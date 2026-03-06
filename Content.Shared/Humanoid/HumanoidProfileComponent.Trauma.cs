using Content.Goobstation.Common.Barks;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - store the profile's bark voice and knowledge settings
/// </summary>
public sealed partial class HumanoidProfileComponent
{
    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    [DataField]
    public KnowledgeProfile Knowledge = new(new());
}
