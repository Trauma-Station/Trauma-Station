using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Trauma.Shared.Body.Markings;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModifyMarkingStatusEffectComponent : Component
{
    [DataField(required: true)]
    public HumanoidVisualLayers Layer;

    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField(required: true)]
    public string Suffix;
}
