using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
public sealed partial class BurnableWingsComponent : Component
{
    [DataField]
    public FixedPoint2 DamageThreshold = 35;

    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Heat";

    [DataField]
    public EntProtoId<OrganComponent> BurntWings = "OrganMothWingsBurntOff";

    [DataField]
    public SoundSpecifier BurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}
