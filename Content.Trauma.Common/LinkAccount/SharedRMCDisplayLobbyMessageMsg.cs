// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.LinkAccount;

[Serializable, NetSerializable]
public sealed class SharedRMCDisplayLobbyMessageEvent(string message, string user) : EntityEventArgs
{
    public readonly string Message = message;
    public readonly string User = user;
}
