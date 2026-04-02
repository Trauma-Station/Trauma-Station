using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

public sealed partial class GameMapPoolPrototype
{
    /// <summary>
    /// Areas that must be mapped on every map in this pool, or tests will fail.
    /// </summary>
    [DataField]
    public List<EntProtoId>? RequiredAreas;
}
