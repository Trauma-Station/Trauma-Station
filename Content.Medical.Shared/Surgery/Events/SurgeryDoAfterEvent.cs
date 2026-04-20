// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly ProtoId<SurgeryPrototype> Surgery;
    public readonly bool ToolUsed;

    public SurgeryDoAfterEvent(ProtoId<SurgeryPrototype> surgery, bool toolUsed)
    {
        Surgery = surgery;
        ToolUsed = toolUsed;
    }
}

[Serializable, NetSerializable]
public sealed partial class DrapeDoAfterEvent : SimpleDoAfterEvent
{
    public DrapeDoAfterEvent() { }
}
