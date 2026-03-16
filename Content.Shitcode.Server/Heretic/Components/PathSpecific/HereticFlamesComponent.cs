using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Components.PathSpecific;

[RegisterComponent]
public sealed partial class HereticFlamesComponent : Component
{
    [DataField]
    public EntProtoId FireProto = "HereticFireAA";

    [ViewVariables]
    public float UpdateTimer;

    [ViewVariables]
    public float LifetimeTimer;

    [DataField]
    public float UpdateDuration = .2f;

    [DataField]
    public float LifetimeDuration = 60f;

    [DataField]
    public int RangeIncrease;

    [DataField]
    public int Range = 1;
}
