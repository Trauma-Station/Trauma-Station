using Content.Shared.FixedPoint;

namespace Content.Shared.Destructible;

public sealed partial class DestructibleComponent
{
    /// <summary>
    /// Scale applied to all damage triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Scale = 1;

    /// <summary>
    /// If false, thresholds won't trigger
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
