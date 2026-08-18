// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Lathe;

/// <summary>
///     Sent to the server when a client resets the lathe's recipe queue
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheQueueResetMessage : BoundUserInterfaceMessage;
