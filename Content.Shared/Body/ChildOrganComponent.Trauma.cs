using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public sealed partial class ChildOrganComponent : Component
{
    /// <summary>
    /// The categories this organ can be a child of.
    /// Usually this is just one for asymmetrical organs.
    /// For symmetrical organs this should be multiple.
    /// </summary>
    [DataField(required: true), AutoNetworkedField, Access(Other = AccessPermissions.ReadExecute)]
    public List<ProtoId<OrganCategoryPrototype>> Parents = new();
}
