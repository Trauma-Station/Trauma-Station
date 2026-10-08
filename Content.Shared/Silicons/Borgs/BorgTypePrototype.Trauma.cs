using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

public sealed partial class BorgTypePrototype
{
    /// <summary>
    /// Lawset to use instead of crewsimov.
    /// If the chassis is emagged or ion stormed this is ignored.
    /// </summary>
    [DataField]
    public ProtoId<SiliconLawsetPrototype>? Lawset;
}
