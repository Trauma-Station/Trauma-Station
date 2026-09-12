using Robust.Shared.Utility;

namespace Content.Client.Communications.UI.Widgets;

public sealed partial class MessagingControls
{
    /// <summary>
    /// Get the reason used for calling/recalling evac.
    /// </summary>
    public string GetEvacReason()
        => Rope.Collapse(RadioMessageInput.TextRope);
}
