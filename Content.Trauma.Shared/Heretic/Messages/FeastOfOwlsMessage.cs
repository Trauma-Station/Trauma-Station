// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Heretic.Messages;

[Serializable, NetSerializable]
public sealed class FeastOfOwlsMessage(bool accepted) : BoundUserInterfaceMessage
{
    public readonly bool Accepted = accepted;
}

[Serializable, NetSerializable]
public enum FeastOfOwlsUiKey : byte
{
    Key
}
