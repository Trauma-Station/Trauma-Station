using Content.Shared.DeviceLinking;

namespace Content.Trauma.Shared.DeviceLinking;

/// <summary>
/// Adds Lock/Unlock/Toggle links and handles their behavior.
/// </summary>
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
