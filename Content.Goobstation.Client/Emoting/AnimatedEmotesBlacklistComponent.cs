using Content.Goobstation.Shared.Emoting;

namespace Content.Goobstation.Client.Emoting;

/// <summary>
/// Blacklists the mob from showing certain visual emotes
/// </summary>
[RegisterComponent]
public sealed partial class AnimatedEmotesBlacklistComponent : Component
{
    [DataField(required: true)]
    public HumanoidVisualEmoteLayers Blacklist = HumanoidVisualEmoteLayers.None;
}
