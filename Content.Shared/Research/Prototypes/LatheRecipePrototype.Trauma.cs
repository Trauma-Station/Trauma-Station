using Content.Shared.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Prototypes;

public sealed partial class LatheRecipePrototype
{
    /// <summary>
    /// Subname displayed in brackets near name. Used for recipes that should have same name, but have some difference.
    /// </summary>
    [DataField("subname")]
    public LocId? SubName;

    /// <summary>
    /// If non-null, the station must be in one of these alert levels for this recipe to be produced.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AlertLevelPrototype>>? RequiredAlerts;
}
