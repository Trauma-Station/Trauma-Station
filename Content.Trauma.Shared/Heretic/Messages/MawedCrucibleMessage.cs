// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Heretic.Messages;

[Serializable, NetSerializable]
public sealed class MawedCrucibleMessage(EntProtoId proto) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Proto = proto;
}

[Serializable, NetSerializable]
public enum MawedCrucibleUiKey : byte
{
    Key
}
