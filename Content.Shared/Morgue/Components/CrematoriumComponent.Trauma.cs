using Robust.Shared.Prototypes;

namespace Content.Shared.Morgue.Components;

public sealed partial class CrematoriumComponent : Component
{
    /// <summary>
    /// Spawned in place of LeftOver if the burned entity was consecrated.
    /// </summary>
    public EntProtoId HolyLeftOverProtoId = "HolyAsh";
}
