using Content.Shared.Tag;

namespace Content.Trauma.Shared.AER;

[RegisterComponent]
public sealed partial class SpendProjectileOnTagComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype> Tag = "Wall";
}