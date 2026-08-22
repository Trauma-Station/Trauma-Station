using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Shared.Light.Components;

public sealed partial class PoweredLightComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> ControlPort = "Control";
}
