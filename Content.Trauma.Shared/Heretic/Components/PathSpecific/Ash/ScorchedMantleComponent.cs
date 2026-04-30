namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScorchedMantleComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "ActionHereticScorchedMantleToggleFlames";

    [DataField]
    public EntityUid? Action;

    [DataField]
    public float FireStackIncreaseMultiplier = 2f;
}
