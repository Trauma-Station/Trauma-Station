using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared._AU14.WorkingJoe;

[RegisterComponent, NetworkedComponent]
public sealed partial class WorkingJoeVoiceComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionWorkingJoeVoice";

    [DataField]
    public EntityUid? ActionEntity;
}
