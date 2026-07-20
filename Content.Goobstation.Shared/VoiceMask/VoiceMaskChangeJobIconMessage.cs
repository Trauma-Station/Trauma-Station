// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Goobstation.Shared.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeJobIconMessage(ProtoId<JobIconPrototype> jobIconProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<JobIconPrototype> JobIconProtoId { get; } = jobIconProtoId;
}
