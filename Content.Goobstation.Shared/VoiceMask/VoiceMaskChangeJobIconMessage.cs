// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeJobIconMessage(ProtoId<JobIconPrototype> jobIconProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<JobIconPrototype> JobIconProtoId { get; } = jobIconProtoId;
}
