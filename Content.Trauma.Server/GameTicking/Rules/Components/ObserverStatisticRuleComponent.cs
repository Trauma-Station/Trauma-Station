using Content.Shared.Roles.Components;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component to track statistics on observers.
/// </summary>
[RegisterComponent, Access(typeof(ObserverStatisticRuleSystem))]
public sealed partial class ObserverStatisticRuleComponent : Component
{
    /// <summary>
    /// Entity with most unique observers
    /// </summary>
    [DataField]
    public EntityUid? MostPopularEntity;

    /// <summary>
    /// Number of unique followers for <see cref="MostPopularEntity"/>
    /// </summary>
    [DataField]
    public int MostPopularEntityPopularity;
}
