using Content.Shared.DeviceLinking;

namespace Content.Trauma.Server.DeviceLinking;

[RegisterComponent]
public sealed partial class LockSignalControlComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> LockPort = "Lock";

    [DataField]
    public ProtoId<SinkPortPrototype> UnlockPort = "Unlock";

    [DataField]
    public ProtoId<SinkPortPrototype> ToggleLockPort = "Toggle";
}
