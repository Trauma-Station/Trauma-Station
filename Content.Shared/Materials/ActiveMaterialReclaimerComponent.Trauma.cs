namespace Content.Shared.Materials;

public sealed partial class ActiveMaterialReclaimerComponent
{
    /// <summary>
    /// List of entities being processed.
    /// </summary>
    [DataField]
    public List<EntityUid> Processing = new();
}
