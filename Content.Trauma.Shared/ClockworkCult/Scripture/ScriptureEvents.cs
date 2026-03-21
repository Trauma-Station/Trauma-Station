using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// Raised when the user presses the Recite button in the UI of the Clockwork Slab
/// </summary>
[Serializable, NetSerializable]
public sealed class ScriptureReciteEvent : BoundUserInterfaceMessage
{
    /// <summary>
    ///  The scripture we want to recite.
    /// </summary>
    public EntProtoId Scripture;

    public ScriptureReciteEvent(EntProtoId scripture)
    {
        Scripture = scripture;
    }
}
