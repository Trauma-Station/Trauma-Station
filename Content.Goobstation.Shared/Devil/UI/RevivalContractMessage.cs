// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Devil.UI;

[Serializable, NetSerializable]
public sealed class RevivalContractMessage(bool accepted) : BoundUserInterfaceMessage
{
    public bool Accepted { get; } = accepted;
}
