namespace Content.Shared.Electrocution;

public sealed partial class ElectrifiedComponent
{
    /// <summary>
    /// Whether this will ignore target insulation
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreInsulation;

    /// <summary>
    /// Don't shock this specific entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? IgnoredEntity;
}
