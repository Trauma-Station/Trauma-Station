using Content.Shared.Actions;
using Content.Shared.Body.Part;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Action component for polymorphing an organ of the performer into a projectile and shooting it at the target.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ShootOrganActionSystem))]
public sealed partial class ShootOrganActionComponent : Component
{
    [DataField(required: true)]
    public BodyPartType PartType;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    [DataField(required: true)]
    public string Organ = string.Empty;

    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}

public sealed partial class ShootOrganActionEvent : WorldTargetActionEvent;
